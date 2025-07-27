namespace PlantSensorDataAPI.Models;

public class SensorReading
{
    public int tray_id { get; set; }
    public double temperature { get; set; }
    public double humidity { get; set; }
    public double light { get; set; }
}
