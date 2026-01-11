namespace Linksoft.PowerController.Controller.RaspberryPi.Domain;

public enum DeviceShutdownState
{
    Pending,
    CommandSent,
    Acknowledged,
    WaitingForPowerOff,
    PoweredOff,
    Failed,
}