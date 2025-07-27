using System.ComponentModel.DataAnnotations.Schema;

namespace PlantSensorDataAPI.Models;

public class CombinedPlantData
{
    public int TrayId { get; set; }
    public string PlantType { get; set; } = string.Empty;

    public double CurrentTemperature { get; set; }
    public double CurrentHumidity { get; set; }
    public double CurrentLight { get; set; }

    public double TargetTemperature { get; set; }
    public double TargetHumidity { get; set; }
    public double TargetLight { get; set; }

    public DateTime Timestamp { get; set; }

}
