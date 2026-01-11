global using System;
global using System.Diagnostics;
global using System.Globalization;
global using System.Runtime.InteropServices;
global using System.Text.Json;
global using System.Threading;
global using System.Threading.Tasks;

global using Atc.DependencyInjection;
global using Atc.SourceGenerators.Annotations;

global using Linksoft.PowerController.HostAgent;
global using Linksoft.PowerController.HostAgent.Configuration;
global using Linksoft.PowerController.HostAgent.Generated.Endpoints;
global using Linksoft.PowerController.HostAgent.Generated.Systems.Handlers;
global using Linksoft.PowerController.HostAgent.Generated.Systems.Models;
global using Linksoft.PowerController.HostAgent.Generated.Systems.Parameters;
global using Linksoft.PowerController.HostAgent.Generated.Systems.Results;
global using Linksoft.PowerController.HostAgent.Services;

global using Microsoft.Extensions.Options;

global using MQTTnet;
global using MQTTnet.Protocol;
global using MQTTnet.Server;

global using Scalar.AspNetCore;

global using Serilog;