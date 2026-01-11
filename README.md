# 🔌 Linksoft.IoT.PowerController

IoT Power Controller system for remote power management across multiple platforms.

## 📦 Components

| Component | Platform | Description |
|-----------|----------|-------------|
| **HostAgent** | Windows/Linux | Central service for power management coordination via REST API and MQTT |
| **Controller.RaspberryPi** | Linux ARM | Edge controller for Raspberry Pi devices |
| **Controller.Esp32** | ESP32 | Embedded controller using nanoFramework |

## 🚀 Quick Start

### HostAgent

```bash
# Run in console mode
dotnet run --project src/Linksoft.PowerController.HostAgent

# Access API documentation
# https://localhost:5100/scalar/v1
```

For production deployment as a Windows Service or Linux daemon, see [HostAgent Installation Guide](docs/HostAgent.md).

### ⚙️ Configuration

The HostAgent supports both REST API and MQTT communication. Configure via `appsettings.json`:

```json
{
  "Mqtt": {
    "Enabled": false,
    "Mode": "External",
    "External": { "Host": "localhost", "Port": 1883 },
    "Topics": { "BaseTopic": "powercontroller", "StatusInterval": 30 }
  }
}
```

Set `Mode` to `"Embedded"` to run a built-in MQTT broker.

## 🛠️ Building

```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build src/Linksoft.PowerController.HostAgent
```

> **Note:** The ESP32 project requires Visual Studio with the nanoFramework extension.

## 🔧 Source Generators

This project leverages compile-time source generation for minimal boilerplate and Native AOT compatibility:

| Generator | Purpose |
|-----------|---------|
| **Atc.Rest.Api.SourceGenerator** | REST API code from OpenAPI specs |
| **Atc.SourceGenerators** | DI registration and options binding |

### Features Used

- **`[Registration]`** - Automatic service registration with conditional support
- **`[OptionsBinding]`** - Configuration class binding with validation

See [CLAUDE.md](CLAUDE.md) for detailed usage examples.

## 📚 Documentation

- [HostAgent Installation Guide](docs/HostAgent.md) - Windows Service and Linux daemon setup

## 📄 License

See [LICENSE](LICENSE) for details.