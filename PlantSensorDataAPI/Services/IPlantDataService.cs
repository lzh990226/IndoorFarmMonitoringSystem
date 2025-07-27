using PlantSensorDataAPI.Models;

namespace PlantSensorDataAPI.Services;

public interface IPlantDataService
{
    List<CombinedPlantData> CombineData(List<SensorReading> sensorReadings, List<PlantConfiguration> plantConfigurations);
    Task<bool> SaveToDatabase(List<CombinedPlantData> combinedData);
    Task<bool> SaveToInMemory(List<CombinedPlantData> combinedData);
    Task<bool> SaveToJsonFile(List<CombinedPlantData> combinedData);
    List<CombinedPlantData> GetFromInMemory();
}
