namespace Linksoft.PowerController.Controller.RaspberryPi.Mapping;

using ApiControllerSystemInfo = Linksoft.PowerController.Controller.RaspberryPi.Generated.Systems.Models.ControllerSystemInfo;
using ApiDevice = Linksoft.PowerController.Controller.RaspberryPi.Generated.Devices.Models.Device;
using ApiDeviceConnectionState = Linksoft.PowerController.Controller.RaspberryPi.Generated.Devices.Models.DeviceConnectionState;
using ApiDeviceShutdownPhase = Linksoft.PowerController.Controller.RaspberryPi.Generated.Shutdowns.Models.DeviceShutdownPhase;
using ApiDeviceShutdownStatus = Linksoft.PowerController.Controller.RaspberryPi.Generated.Shutdowns.Models.DeviceShutdownStatus;
using ApiDeviceStatus = Linksoft.PowerController.Controller.RaspberryPi.Generated.Devices.Models.DeviceStatus;
using ApiDeviceType = Linksoft.PowerController.Controller.RaspberryPi.Generated.Devices.Models.DeviceType;
using ApiEndpointType = Linksoft.PowerController.Controller.RaspberryPi.Generated.Devices.Models.EndpointType;
using ApiShutdownProgress = Linksoft.PowerController.Controller.RaspberryPi.Generated.Shutdowns.Models.ShutdownProgress;
using ApiShutdownState = Linksoft.PowerController.Controller.RaspberryPi.Generated.Models.ShutdownState;

/// <summary>
/// Maps between domain entities and API models.
/// </summary>
public static class TypeMapper
{
    public static ApiDevice ToApiDevice(Domain.DeviceEntity device)
    {
        ArgumentNullException.ThrowIfNull(device);

        return new ApiDevice(
            Id: device.Id,
            Name: device.Name,
            IpAddress: device.IpAddress,
            Type: ToApiDeviceType(device.Type),
            EndpointType: device.EndpointType.HasValue ? ToApiEndpointType(device.EndpointType.Value) : default,
            Port: device.Port,
            ShutdownOrder: device.ShutdownOrder,
            RelayId: device.RelayId,
            Enabled: device.Enabled);
    }

    public static ApiDeviceType ToApiDeviceType(Domain.DeviceType type)
        => type switch
        {
            Domain.DeviceType.HostAgent => ApiDeviceType.HostAgent,
            Domain.DeviceType.EsuEcos50210 => ApiDeviceType.EsuEcos50210,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };

    public static Domain.DeviceType ToDomainDeviceType(ApiDeviceType type)
        => type switch
        {
            ApiDeviceType.HostAgent => Domain.DeviceType.HostAgent,
            ApiDeviceType.EsuEcos50210 => Domain.DeviceType.EsuEcos50210,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };

    public static ApiEndpointType ToApiEndpointType(Domain.EndpointType type)
        => type switch
        {
            Domain.EndpointType.RestApi => ApiEndpointType.RestApi,
            Domain.EndpointType.Mqtt => ApiEndpointType.Mqtt,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };

    public static Domain.EndpointType ToDomainEndpointType(ApiEndpointType type)
        => type switch
        {
            ApiEndpointType.RestApi => Domain.EndpointType.RestApi,
            ApiEndpointType.Mqtt => Domain.EndpointType.Mqtt,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };

    public static ApiDeviceStatus ToApiDeviceStatus(
        Domain.DeviceStatusEntity status)
    {
        ArgumentNullException.ThrowIfNull(status);

        return new ApiDeviceStatus(
            DeviceId: status.DeviceId,
            DeviceName: status.DeviceName,
            ConnectionState: ToApiDeviceConnectionState(status.ConnectionState),
            ShutdownInProgress: status.ShutdownInProgress,
            ShutdownScheduledAt: status.ShutdownScheduledAt ?? default,
            ServiceUptime: status.ServiceUptime ?? string.Empty,
            ServerUptime: status.ServerUptime ?? string.Empty,
            Hostname: status.Hostname ?? string.Empty,
            OperatingSystem: status.OperatingSystem ?? string.Empty,
            LastChecked: status.LastChecked,
            LastError: status.LastError ?? string.Empty);
    }

    public static ApiDeviceConnectionState ToApiDeviceConnectionState(
        Domain.DeviceConnectionState state)
        => state switch
        {
            Domain.DeviceConnectionState.Unknown => ApiDeviceConnectionState.Unknown,
            Domain.DeviceConnectionState.Online => ApiDeviceConnectionState.Online,
            Domain.DeviceConnectionState.ShuttingDown => ApiDeviceConnectionState.ShuttingDown,
            Domain.DeviceConnectionState.NotResponding => ApiDeviceConnectionState.NotResponding,
            Domain.DeviceConnectionState.PoweredOff => ApiDeviceConnectionState.PoweredOff,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
        };

    public static ApiShutdownProgress ToApiShutdownProgress(
        Domain.ShutdownProgressEntity progress)
    {
        ArgumentNullException.ThrowIfNull(progress);

        return new ApiShutdownProgress(
            State: ToApiShutdownState(progress.State),
            StartedAt: progress.StartedAt,
            CompletedAt: progress.CompletedAt ?? default,
            DeviceStatuses: progress
                .DeviceStatuses
                .Select(ToApiDeviceShutdownStatus)
                .ToArray(),
            ErrorMessage: progress.ErrorMessage ?? string.Empty,
            CanCancel: progress.CanCancel);
    }

    public static ApiShutdownState ToApiShutdownState(
        Domain.ShutdownState state)
        => state switch
        {
            Domain.ShutdownState.Idle => ApiShutdownState.Idle,
            Domain.ShutdownState.Initiating => ApiShutdownState.Initiating,
            Domain.ShutdownState.Monitoring => ApiShutdownState.Monitoring,
            Domain.ShutdownState.ReadyForPowerCut => ApiShutdownState.ReadyForPowerCut,
            Domain.ShutdownState.PowerCutExecuted => ApiShutdownState.PowerCutExecuted,
            Domain.ShutdownState.Cancelled => ApiShutdownState.Cancelled,
            Domain.ShutdownState.Failed => ApiShutdownState.Failed,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
        };

    public static ApiDeviceShutdownStatus ToApiDeviceShutdownStatus(
        Domain.DeviceShutdownStatusEntity status)
    {
        ArgumentNullException.ThrowIfNull(status);

        return new ApiDeviceShutdownStatus(
            DeviceId: status.DeviceId,
            DeviceName: status.DeviceName,
            RelayId: status.RelayId,
            Phase: ToApiDeviceShutdownPhase(status.Phase),
            CommandSentAt: status.CommandSentAt ?? default,
            AcknowledgedAt: status.AcknowledgedAt ?? default,
            PoweredOffAt: status.PoweredOffAt ?? default,
            Error: status.Error ?? string.Empty);
    }

    public static ApiDeviceShutdownPhase ToApiDeviceShutdownPhase(
        Domain.DeviceShutdownState phase)
        => phase switch
        {
            Domain.DeviceShutdownState.Pending => ApiDeviceShutdownPhase.Pending,
            Domain.DeviceShutdownState.CommandSent => ApiDeviceShutdownPhase.CommandSent,
            Domain.DeviceShutdownState.Acknowledged => ApiDeviceShutdownPhase.Acknowledged,
            Domain.DeviceShutdownState.WaitingForPowerOff => ApiDeviceShutdownPhase.WaitingForPowerOff,
            Domain.DeviceShutdownState.PoweredOff => ApiDeviceShutdownPhase.PoweredOff,
            Domain.DeviceShutdownState.Failed => ApiDeviceShutdownPhase.Failed,
            _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, null),
        };

    public static ApiControllerSystemInfo ToApiControllerSystemInfo(
        string serviceUptime,
        string serverUptime,
        string hostname,
        string operatingSystem,
        Domain.ShutdownState shutdownState,
        int deviceCount,
        bool mqttConnected)
        => new(
            ServiceUptime: serviceUptime,
            ServerUptime: serverUptime,
            Hostname: hostname,
            OperatingSystem: operatingSystem,
            ShutdownState: ToApiShutdownState(shutdownState),
            DeviceCount: deviceCount,
            MqttConnected: mqttConnected);
}