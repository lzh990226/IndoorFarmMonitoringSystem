namespace PlantSensorDataAPI.Configuration;

public class ExternalApiSettings
{
    public const string SectionName = "ExternalApis";

    public string SensorReadingsUrl { get; set; } = string.Empty;
    public string PlantConfigurationsUrl { get; set; } = string.Empty;
}
