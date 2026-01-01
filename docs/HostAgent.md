# 🖥️ HostAgent Installation Guide

The HostAgent is a cross-platform service that can run as:
- **Windows Service** on Windows
- **systemd daemon** on Linux
- **Console application** for development/debugging

## 🛠️ Building

```bash
# Build for current platform
dotnet build src/Linksoft.PowerController.HostAgent

# Publish self-contained executable
dotnet publish src/Linksoft.PowerController.HostAgent -c Release -o ./publish
```

## ⚙️ Configuration

The HostAgent is configured via `appsettings.json`:

```json
{
  "Mqtt": {
    "Enabled": false,
    "Mode": "External",
    "External": {
      "Host": "localhost",
      "Port": 1883,
      "UseTls": false
    },
    "Embedded": {
      "Port": 1883
    },
    "Topics": {
      "BaseTopic": "powercontroller",
      "StatusInterval": 30
    }
  }
}
```

### 📡 MQTT Modes

- **External**: Connect to an existing MQTT broker
- **Embedded**: Run a built-in MQTT broker (set `Mode` to `"Embedded"`)

## 💻 Running as Console (Development)

```bash
dotnet run --project src/Linksoft.PowerController.HostAgent
```

The application will run in the foreground and can be stopped with `Ctrl+C`.

## 🪟 Windows Service Installation

### Prerequisites

- Windows 10/11 or Windows Server 2016+
- Administrator privileges

### Installation Steps

1. **Publish the application:**

   ```powershell
   dotnet publish src/Linksoft.PowerController.HostAgent -c Release -o C:\Services\LinksoftPowerController
   ```

2. **Create the Windows Service:**

   ```powershell
   sc.exe create "LinksoftPowerController" `
       binPath="C:\Services\LinksoftPowerController\Linksoft.PowerController.HostAgent.exe" `
       start=auto `
       displayname="Linksoft Power Controller"
   ```

3. **Set service description (optional):**

   ```powershell
   sc.exe description "LinksoftPowerController" "IoT Power Controller Host Agent - manages remote power control via REST API and MQTT"
   ```

4. **Start the service:**

   ```powershell
   sc.exe start "LinksoftPowerController"
   ```

### Service Management

```powershell
# Check status
sc.exe query "LinksoftPowerController"

# Stop service
sc.exe stop "LinksoftPowerController"

# Remove service
sc.exe delete "LinksoftPowerController"
```

### Logs

Logs are written to `logs/hostagent-YYYYMMDD.log` in the application directory.

## 🐧 Linux systemd Installation

### Prerequisites

- Linux with systemd (Ubuntu 18.04+, Debian 10+, RHEL 8+, etc.)
- .NET Runtime 10.0 or self-contained publish

### Installation Steps

1. **Publish the application:**

   ```bash
   dotnet publish src/Linksoft.PowerController.HostAgent -c Release -o /opt/linksoft/powercontroller
   ```

2. **Set permissions:**

   ```bash
   sudo chmod +x /opt/linksoft/powercontroller/Linksoft.PowerController.HostAgent
   ```

3. **Create systemd service file:**

   ```bash
   sudo nano /etc/systemd/system/linksoft-powercontroller.service
   ```

   Add the following content:

   ```ini
   [Unit]
   Description=Linksoft Power Controller Host Agent
   Documentation=https://github.com/Linksoft/Linksoft.IoT.PowerController
   After=network.target

   [Service]
   Type=notify
   ExecStart=/opt/linksoft/powercontroller/Linksoft.PowerController.HostAgent
   WorkingDirectory=/opt/linksoft/powercontroller
   Restart=always
   RestartSec=10
   User=root
   Environment=DOTNET_ENVIRONMENT=Production
   Environment=ASPNETCORE_URLS=http://*:5000

   [Install]
   WantedBy=multi-user.target
   ```

4. **Enable and start the service:**

   ```bash
   sudo systemctl daemon-reload
   sudo systemctl enable linksoft-powercontroller
   sudo systemctl start linksoft-powercontroller
   ```

### Service Management

```bash
# Check status
sudo systemctl status linksoft-powercontroller

# View logs
sudo journalctl -u linksoft-powercontroller -f

# Stop service
sudo systemctl stop linksoft-powercontroller

# Restart service
sudo systemctl restart linksoft-powercontroller

# Disable service
sudo systemctl disable linksoft-powercontroller
```

### Logs

- **File logs**: `/opt/linksoft/powercontroller/logs/hostagent-YYYYMMDD.log`
- **Journal logs**: `journalctl -u linksoft-powercontroller`

## 🔗 API Endpoints

Once running, the HostAgent exposes:

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/system/info` | GET | Get system information (uptime, hostname, shutdown status) |
| `/system/shutdown` | POST | Initiate shutdown (modes: Immediate, Delayed, Scheduled) |
| `/scalar/v1` | GET | Interactive API documentation |

## 🔥 Firewall Configuration

### Windows

```powershell
New-NetFirewallRule -DisplayName "Linksoft Power Controller" -Direction Inbound -Port 5000 -Protocol TCP -Action Allow
```

### Linux

```bash
sudo ufw allow 5000/tcp
```

## 🔧 Troubleshooting

### Service won't start

1. Check logs in the `logs/` directory
2. Verify `appsettings.json` is valid JSON
3. Ensure the port (default 5000) is not in use

### MQTT connection issues

1. Verify broker is reachable: `telnet <host> 1883`
2. Check MQTT configuration in `appsettings.json`
3. Review logs for connection errors

### Permission denied (Linux)

Ensure the service has appropriate permissions:

```bash
sudo chown -R root:root /opt/linksoft/powercontroller
sudo chmod +x /opt/linksoft/powercontroller/Linksoft.PowerController.HostAgent
```
