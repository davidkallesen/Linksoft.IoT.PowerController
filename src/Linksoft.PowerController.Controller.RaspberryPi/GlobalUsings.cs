global using System;
global using System.Collections.Concurrent;
global using System.Device.Gpio;
global using System.Globalization;
global using System.Net;
global using System.Net.NetworkInformation;
global using System.Runtime.InteropServices;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Threading;
global using System.Threading.Tasks;

global using Atc.DependencyInjection;
global using Atc.Network.Helpers;
global using Atc.Network.Internet;
global using Atc.Network.Models;
global using Atc.SourceGenerators.Annotations;

global using Linksoft.PowerController.Controller.RaspberryPi;
global using Linksoft.PowerController.Controller.RaspberryPi.Configuration;
global using Linksoft.PowerController.Controller.RaspberryPi.Generated.Devices.Handlers;
global using Linksoft.PowerController.Controller.RaspberryPi.Generated.Devices.Parameters;
global using Linksoft.PowerController.Controller.RaspberryPi.Generated.Devices.Results;
global using Linksoft.PowerController.Controller.RaspberryPi.Generated.Endpoints;
global using Linksoft.PowerController.Controller.RaspberryPi.Generated.Models;
global using Linksoft.PowerController.Controller.RaspberryPi.Generated.Shutdowns.Handlers;
global using Linksoft.PowerController.Controller.RaspberryPi.Generated.Shutdowns.Parameters;
global using Linksoft.PowerController.Controller.RaspberryPi.Generated.Shutdowns.Results;
global using Linksoft.PowerController.Controller.RaspberryPi.Generated.Systems.Handlers;
global using Linksoft.PowerController.Controller.RaspberryPi.Generated.Systems.Results;
global using Linksoft.PowerController.Controller.RaspberryPi.Mapping;
global using Linksoft.PowerController.Controller.RaspberryPi.Models;
global using Linksoft.PowerController.Controller.RaspberryPi.Services;

global using Microsoft.Extensions.Options;

global using MQTTnet;
global using MQTTnet.Protocol;

global using Scalar.AspNetCore;

global using Serilog;