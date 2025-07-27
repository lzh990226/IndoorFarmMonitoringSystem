using PlantSensorDataAPI.Models;

namespace PlantSensorDataAPI.Services;

public interface IExternalApiService
{
    Task<List<SensorReading>?> GetSensorReadingsAsync();
    Task<List<PlantConfiguration>?> GetPlantConfigurationsAsync();
}
