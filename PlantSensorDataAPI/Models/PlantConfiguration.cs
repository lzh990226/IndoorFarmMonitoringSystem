namespace PlantSensorDataAPI.Models;

public class PlantConfiguration
{
    public int tray_id { get; set; }
    public string plant_type { get; set; } = string.Empty;
    public double target_temperature { get; set; }
    public double target_humidity { get; set; }
    public double target_light { get; set; }
}
