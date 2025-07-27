using Microsoft.EntityFrameworkCore;
using PlantSensorDataAPI.Data;
using PlantSensorDataAPI.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace PlantSensorDataAPI.Services;

public class PlantDataService : IPlantDataService
{
    private readonly PlantSensorDbContext _dbContext;
    private readonly ILogger<PlantDataService> _logger;

    // In-memory storage using thread-safe dictionary
    private static readonly ConcurrentDictionary<int, CombinedPlantData> _inMemoryStorage = new();

    // JSON file path
    private const string JsonFilePath = "Data/plant-sensor-data.json";

    public PlantDataService(PlantSensorDbContext dbContext, ILogger<PlantDataService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;

        // Ensure Data directory exists for JSON storage
        var dataDirectory = Path.GetDirectoryName(JsonFilePath);
        if (!string.IsNullOrEmpty(dataDirectory) && !Directory.Exists(dataDirectory))
        {
            Directory.CreateDirectory(dataDirectory);
        }
    }

    public List<CombinedPlantData> CombineData(List<SensorReading> sensorReadings, List<PlantConfiguration> plantConfigurations)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Starting data combination");

        var combinedData = new List<CombinedPlantData>();

        foreach (var config in plantConfigurations)
        {
            var sensorData = sensorReadings.FirstOrDefault(s => s.tray_id == config.tray_id);

            if (sensorData != null)
            {
                var combined = new CombinedPlantData
                {
                    TrayId = config.tray_id,
                    PlantType = config.plant_type,
                    CurrentTemperature = sensorData.temperature,
                    CurrentHumidity = sensorData.humidity,
                    CurrentLight = sensorData.light,
                    TargetTemperature = config.target_temperature,
                    TargetHumidity = config.target_humidity,
                    TargetLight = config.target_light,
                    Timestamp = DateTime.UtcNow
                };

                combinedData.Add(combined);
            }
            else
            {
                _logger.LogWarning("No sensor data found for tray_id {TrayId}", config.tray_id);
            }
        }

        stopwatch.Stop();
        _logger.LogInformation("Data combination completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

        return combinedData;
    }

    public async Task<bool> SaveToDatabase(List<CombinedPlantData> combinedData)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Starting database upsert operation for {RecordCount} records", combinedData.Count);

        try
        {
            var updatedRecords = 0;
            var insertedRecords = 0;

            foreach (var data in combinedData)
            {
                var existing = await _dbContext.CombinedPlantData
                    .FirstOrDefaultAsync(x => x.TrayId == data.TrayId);

                if (existing != null)
                {
                    _logger.LogDebug("Updating existing record for tray {TrayId}", data.TrayId);

                    existing.PlantType = data.PlantType;
                    existing.CurrentTemperature = data.CurrentTemperature;
                    existing.CurrentHumidity = data.CurrentHumidity;
                    existing.CurrentLight = data.CurrentLight;
                    existing.TargetTemperature = data.TargetTemperature;
                    existing.TargetHumidity = data.TargetHumidity;
                    existing.TargetLight = data.TargetLight;
                    existing.Timestamp = data.Timestamp;

                    _dbContext.CombinedPlantData.Update(existing);
                    updatedRecords++;
                }
                else
                {
                    _logger.LogDebug("Inserting new record for tray {TrayId}", data.TrayId);
                    await _dbContext.CombinedPlantData.AddAsync(data);
                    insertedRecords++;
                }
            }

            await _dbContext.SaveChangesAsync();
            stopwatch.Stop();

            _logger.LogInformation("Database upsert completed successfully in {ElapsedMs}ms. Updated: {UpdatedCount}, Inserted: {InsertedCount}",
                stopwatch.ElapsedMilliseconds, updatedRecords, insertedRecords);

            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Database concurrency error while saving plant data after {ElapsedMs}ms",
                stopwatch.ElapsedMilliseconds);
            return false;
        }
        catch (DbUpdateException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Database update error while saving plant data after {ElapsedMs}ms",
                stopwatch.ElapsedMilliseconds);
            return false;
        }
        catch (InvalidOperationException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Database connection error while saving plant data after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            return false;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Unexpected error saving data to database after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            return false;
        }
    }

    public Task<bool> SaveToInMemory(List<CombinedPlantData> combinedData)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Starting in-memory storage operation for {RecordCount} records", combinedData.Count);

        try
        {
            var updatedRecords = 0;
            var insertedRecords = 0;

            foreach (var data in combinedData)
            {
                var existingData = _inMemoryStorage.ContainsKey(data.TrayId);

                if (existingData)
                {
                    _logger.LogDebug("Updating existing in-memory record for tray {TrayId}", data.TrayId);
                    _inMemoryStorage[data.TrayId] = new CombinedPlantData
                    {
                        TrayId = data.TrayId,
                        PlantType = data.PlantType,
                        CurrentTemperature = data.CurrentTemperature,
                        CurrentHumidity = data.CurrentHumidity,
                        CurrentLight = data.CurrentLight,
                        TargetTemperature = data.TargetTemperature,
                        TargetHumidity = data.TargetHumidity,
                        TargetLight = data.TargetLight,
                        Timestamp = data.Timestamp
                    };
                    updatedRecords++;
                }
                else
                {
                    _logger.LogDebug("Inserting new in-memory record for tray {TrayId}", data.TrayId);
                    _inMemoryStorage.TryAdd(data.TrayId, new CombinedPlantData
                    {
                        TrayId = data.TrayId,
                        PlantType = data.PlantType,
                        CurrentTemperature = data.CurrentTemperature,
                        CurrentHumidity = data.CurrentHumidity,
                        CurrentLight = data.CurrentLight,
                        TargetTemperature = data.TargetTemperature,
                        TargetHumidity = data.TargetHumidity,
                        TargetLight = data.TargetLight,
                        Timestamp = data.Timestamp
                    });
                    insertedRecords++;
                }
            }

            stopwatch.Stop();
            _logger.LogInformation("In-memory storage completed successfully in {ElapsedMs}ms. Updated: {UpdatedCount}, Inserted: {InsertedCount}",
                stopwatch.ElapsedMilliseconds, updatedRecords, insertedRecords);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Unexpected error saving data to in-memory storage after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            return Task.FromResult(false);
        }
    }
    public async Task<bool> SaveToJsonFile(List<CombinedPlantData> combinedData)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Starting JSON file storage operation for {RecordCount} records", combinedData.Count);

        try
        {
            // Read existing data if file exists
            List<CombinedPlantData> existingData = new();
            if (File.Exists(JsonFilePath))
            {
                var existingJson = await File.ReadAllTextAsync(JsonFilePath);
                if (!string.IsNullOrWhiteSpace(existingJson))
                {
                    existingData = JsonSerializer.Deserialize<List<CombinedPlantData>>(existingJson) ?? new List<CombinedPlantData>();
                }
            }

            // Convert to dictionary for easier upsert operation
            var dataDict = existingData.ToDictionary(x => x.TrayId, x => x);
            var updatedRecords = 0;
            var insertedRecords = 0;

            // Update or insert new data
            foreach (var data in combinedData)
            {
                if (dataDict.ContainsKey(data.TrayId))
                {
                    _logger.LogDebug("Updating existing JSON record for tray {TrayId}", data.TrayId);
                    dataDict[data.TrayId] = data;
                    updatedRecords++;
                }
                else
                {
                    _logger.LogDebug("Inserting new JSON record for tray {TrayId}", data.TrayId);
                    dataDict[data.TrayId] = data;
                    insertedRecords++;
                }
            }

            // Convert back to list and serialize with formatting
            var updatedDataList = dataDict.Values.ToList();
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(updatedDataList, options);
            await File.WriteAllTextAsync(JsonFilePath, json);

            stopwatch.Stop();
            _logger.LogInformation("JSON file storage completed successfully in {ElapsedMs}ms. Updated: {UpdatedCount}, Inserted: {InsertedCount}",
                stopwatch.ElapsedMilliseconds, updatedRecords, insertedRecords);

            return true;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Unexpected error saving data to JSON file after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            return false;
        }
    }

    public List<CombinedPlantData> GetFromInMemory()
    {
        _logger.LogInformation("Retrieving {RecordCount} records from in-memory storage", _inMemoryStorage.Count);
        return _inMemoryStorage.Values.ToList();
    }
}
