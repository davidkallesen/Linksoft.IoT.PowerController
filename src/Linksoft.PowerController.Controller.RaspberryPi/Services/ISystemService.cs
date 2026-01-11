namespace Linksoft.PowerController.Controller.RaspberryPi.Services;

/// <summary>
/// Service for retrieving controller system information.
/// </summary>
public interface ISystemService
{
    /// <summary>
    /// Gets the controller system information.
    /// </summary>
    Linksoft.PowerController.Controller.RaspberryPi.Generated.Systems.Models.ControllerSystemInfo GetSystemInfo();
}