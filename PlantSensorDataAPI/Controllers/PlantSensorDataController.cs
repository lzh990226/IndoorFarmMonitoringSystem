using Microsoft.AspNetCore.Mvc;
using PlantSensorDataAPI.Services;
using System.Diagnostics;

namespace PlantSensorDataAPI.Controllers;

[ApiController]
[Route("plant-sensor-data")]
public class PlantSensorDataController : ControllerBase
{
    private readonly ILogger<PlantSensorDataController> _logger;
    private readonly IExternalApiService _externalApiService;
    private readonly IPlantDataService _plantDataService;

    public PlantSensorDataController(
        ILogger<PlantSensorDataController> logger,
        IExternalApiService externalApiService,
        IPlantDataService plantDataService)
    {
        _logger = logger;
        _externalApiService = externalApiService;
        _plantDataService = plantDataService;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? storage = null)
    {
        var requestStopwatch = Stopwatch.StartNew();

        try
        {
            // Get data from API
            var sensorReadings = await _externalApiService.GetSensorReadingsAsync();
            if (sensorReadings == null)
            {
                _logger.LogError("Failed to retrieve sensor readings");
                return StatusCode(500, new { error = "Failed to retrieve sensor readings from external service" });
            }

            var plantConfigurations = await _externalApiService.GetPlantConfigurationsAsync();
            if (plantConfigurations == null)
            {
                _logger.LogError("Failed to retrieve plant configurations");
                return StatusCode(500, new { error = "Failed to retrieve plant configurations from external service" });
            }

            // Combine the data
            var combinedData = _plantDataService.CombineData(sensorReadings, plantConfigurations);

            // Determine storage method based on optional parameter
            bool saveSuccess = true;
            string storageUsed = "None";

            if (!string.IsNullOrEmpty(storage))
            {
                storageUsed = storage;
                switch (storage.ToLower())
                {
                    case "postgresql":
                        saveSuccess = await _plantDataService.SaveToDatabase(combinedData);
                        _logger.LogInformation("Data saved to PostgreSQL database");
                        break;
                    case "inmemory":
                        saveSuccess = await _plantDataService.SaveToInMemory(combinedData);
                        _logger.LogInformation("Data saved to in-memory storage");
                        break;

                    case "json":
                        saveSuccess = await _plantDataService.SaveToJsonFile(combinedData);
                        _logger.LogInformation("Data saved to JSON file");
                        break;

                    default:
                        _logger.LogWarning("Unknown storage type '{StorageType}', data not saved", storage);
                        saveSuccess = false;
                        storageUsed = $"Unknown({storage})";
                        break;
                }
            }
            else
            {
                _logger.LogInformation("No storage parameter provided, data not saved to any storage");
            }

            requestStopwatch.Stop();
            _logger.LogInformation("Plant sensor data request completed successfully in {ElapsedMs}ms", requestStopwatch.ElapsedMilliseconds);

            return Ok(new
            {
                success = true,
                data = combinedData,
                saved = saveSuccess && !string.IsNullOrEmpty(storage),
                storageType = storageUsed,
                processingTimeMs = requestStopwatch.ElapsedMilliseconds
            });
        }
        catch (Exception ex)
        {
            requestStopwatch.Stop();
            _logger.LogError(ex, "Unexpected error processing plant sensor data request after {ElapsedMs}ms",
                requestStopwatch.ElapsedMilliseconds);

            return StatusCode(500, new
            {
                error = "An unexpected error occurred while processing the request",
                processingTimeMs = requestStopwatch.ElapsedMilliseconds
            });
        }
    }

    [HttpGet("verify-storage")]
    public IActionResult VerifyStorage()
    {
        try
        {
            var inMemoryData = _plantDataService.GetFromInMemory();
            return Ok(new
            {
                storageType = "InMemory",
                recordCount = inMemoryData.Count,
                data = inMemoryData,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying storage");
            return StatusCode(500, new
            {
                error = "An error occurred while verifying storage",
            });
        }
    }
}
