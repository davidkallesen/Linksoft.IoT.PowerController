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
├── Linksoft.PowerController.Controller.RaspberryPi/  # .NET 10 console app
└── Linksoft.PowerController.Controller.Esp32/    # nanoFramework embedded
```

The HostAgent coordinates with controllers via REST API and/or MQTT. Controllers run on edge devices (Raspberry Pi, ESP32) to manage local power states.
