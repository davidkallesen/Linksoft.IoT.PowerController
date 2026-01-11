namespace Linksoft.PowerController.Controller.RaspberryPi.Domain;

public enum ShutdownState
{
    Idle,
    Initiating,
    Monitoring,
    ReadyForPowerCut,
    PowerCutExecuted,
    Cancelled,
    Failed,
}