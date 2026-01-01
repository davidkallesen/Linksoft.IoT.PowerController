# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

IoT Power Controller system with multi-platform support:
- **HostAgent** - Windows/Linux service for remote power management and coordination
- **Controller.RaspberryPi** - Linux ARM-based power controller
- **Controller.Esp32** - nanoFramework embedded controller for ESP32 microcontrollers

## Build Commands

```bash
# Build entire solution
dotnet build ../Linksoft.PowerController.slnx

# Build specific project
dotnet build Linksoft.PowerController.HostAgent/
dotnet build Linksoft.PowerController.Controller.RaspberryPi/

# Run specific project
dotnet run --project Linksoft.PowerController.HostAgent/
dotnet run --project Linksoft.PowerController.Controller.RaspberryPi/
```

Note: The ESP32 project uses nanoFramework and requires Visual Studio with the nanoFramework extension for building and deployment.

## Coding Standards

This project uses ATC Coding Rules (DotNet10 distribution). Key requirements:
- File-scoped namespaces (required)
- Nullable reference types enabled
- Implicit usings enabled
- Most analyzer warnings treated as errors

Update coding rules with:
```powershell
../atc-coding-rules-updater.ps1
```

## Architecture

```
src/
├── Linksoft.PowerController.HostAgent/           # .NET 10 Windows/Linux service
│   ├── ApiHandlers/                              # REST API request handlers
│   ├── Configuration/                            # MQTT and app configuration models
│   ├── Services/                                 # Business logic and MQTT services
│   └── wwwroot/                                  # Static UI (index.html)
├── Linksoft.PowerController.Controller.RaspberryPi/  # .NET 10 console app
└── Linksoft.PowerController.Controller.Esp32/    # nanoFramework embedded
```

The HostAgent coordinates with controllers via REST API and/or MQTT. Controllers run on edge devices (Raspberry Pi, ESP32) to manage local power states.

## HostAgent API

Uses `atc-rest-api-source-generator` with OpenAPI spec (`HostAgent.yaml`). Generated code in `obj/Generated/`.

### REST Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/system/info` | GET | Get system information (uptime, hostname, shutdown status) |
| `/system/shutdown` | POST | Initiate shutdown (modes: Immediate, Delayed, Scheduled) |
| `/scalar/v1` | GET | Interactive API documentation |

### MQTT Topics (when enabled)

| Topic | Direction | Description |
|-------|-----------|-------------|
| `powercontroller/{hostname}/status` | Publish | Auto-published status (configurable interval) |
| `powercontroller/{hostname}/info/request` | Subscribe | Request system info |
| `powercontroller/{hostname}/info/response` | Publish | System info response |
| `powercontroller/{hostname}/shutdown/request` | Subscribe | Receive shutdown commands |
| `powercontroller/{hostname}/shutdown/response` | Publish | Shutdown confirmation |

### Configuration (appsettings.json)

```json
{
  "Mqtt": {
    "Enabled": false,
    "Mode": "External",
    "External": { "Host": "localhost", "Port": 1883, "UseTls": false },
    "Embedded": { "Port": 1883 },
    "Topics": { "BaseTopic": "powercontroller", "StatusInterval": 30 }
  }
}
```

Set `Mode` to `"Embedded"` to run a built-in MQTT broker.

## Key Dependencies

- **Atc.Rest.Api.SourceGenerator** - OpenAPI-based code generation
- **MQTTnet** / **MQTTnet.Server** - MQTT client and embedded broker
- **Serilog** - Structured logging to file (`logs/hostagent-*.log`)
- **Scalar.AspNetCore** - API documentation UI
