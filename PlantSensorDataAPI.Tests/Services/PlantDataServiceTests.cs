using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PlantSensorDataAPI.Data;
using PlantSensorDataAPI.Models;
using PlantSensorDataAPI.Services;

namespace PlantSensorDataAPI.Tests.Services;

public class PlantDataServiceTests : IDisposable
{
    private readonly PlantSensorDbContext _context;
    private readonly Mock<ILogger<PlantDataService>> _loggerMock;
    private readonly PlantDataService _plantDataService;

    public PlantDataServiceTests()
    {
        var options = new DbContextOptionsBuilder<PlantSensorDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PlantSensorDbContext(options);
        _loggerMock = new Mock<ILogger<PlantDataService>>();
        _plantDataService = new PlantDataService(_context, _loggerMock.Object);
    }

    #region CombineData Tests - Success Scenarios

    [Fact]
    public void CombineData_WithMatchingData_ReturnsCorrectCombination()
    {
        // Data setup
        var sensorReadings = new List<SensorReading>
        {
            new() { tray_id = 1, temperature = 22.5, humidity = 65.0, light = 1200.0 },
            new() { tray_id = 2, temperature = 24.0, humidity = 70.0, light = 1100.0 }
        };
        var plantConfigurations = new List<PlantConfiguration>
        {
            new() { tray_id = 1, plant_type = "Lettuce", target_temperature = 23.0, target_humidity = 70.0, target_light = 1000.0 },
            new() { tray_id = 2, plant_type = "Tomato", target_temperature = 25.0, target_humidity = 65.0, target_light = 1200.0 }
        };

        // function call
        var result = _plantDataService.CombineData(sensorReadings, plantConfigurations);

        // start testing
        Assert.Equal(2, result.Count);

        var first = result.First(x => x.TrayId == 1);
        Assert.Equal("Lettuce", first.PlantType);
        Assert.Equal(22.5, first.CurrentTemperature);
        Assert.Equal(23.0, first.TargetTemperature);
        Assert.Equal(65.0, first.CurrentHumidity);
        Assert.Equal(70.0, first.TargetHumidity);
        Assert.Equal(1200.0, first.CurrentLight);
        Assert.Equal(1000.0, first.TargetLight);

        var second = result.First(x => x.TrayId == 2);
        Assert.Equal("Tomato", second.PlantType);
        Assert.Equal(24.0, second.CurrentTemperature);
        Assert.Equal(25.0, second.TargetTemperature);
        Assert.Equal(70.0, second.CurrentHumidity);
        Assert.Equal(65.0, second.TargetHumidity);
        Assert.Equal(1100.0, second.CurrentLight);
        Assert.Equal(1200.0, second.TargetLight);
    }

    #endregion

    #region CombineData Tests - Missing Data Scenarios

    [Fact]
    public void CombineData_WithMissingSensorData_LogsWarningAndSkips()
    {
        // Data setup
        var sensorReadings = new List<SensorReading>
        {
            new() { tray_id = 1, temperature = 22.5, humidity = 65.0, light = 1200.0 }
        };

        var plantConfigurations = new List<PlantConfiguration>
        {
            new() { tray_id = 1, plant_type = "Lettuce", target_temperature = 23.0, target_humidity = 70.0, target_light = 1000.0 },
            new() { tray_id = 2, plant_type = "Tomato", target_temperature = 25.0, target_humidity = 65.0, target_light = 1200.0 }
        };

        // function call
        var result = _plantDataService.CombineData(sensorReadings, plantConfigurations);

        // start testing
        Assert.Single(result);
        Assert.Equal(1, result.First().TrayId);

        // Verify warning was logged for missing tray
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No sensor data found for tray_id 2")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void CombineData_WithEmptyLists_ReturnsEmptyResult()
    {
        // Data setup
        var sensorReadings = new List<SensorReading>();
        var plantConfigurations = new List<PlantConfiguration>();

        // Function call
        var result = _plantDataService.CombineData(sensorReadings, plantConfigurations);

        // Start testing
        Assert.Empty(result);
    }

    [Fact]
    public void CombineData_WithEmptySensorReadings_ReturnsEmptyResult()
    {
        // Data setup
        var sensorReadings = new List<SensorReading>();
        var plantConfigurations = new List<PlantConfiguration>
        {
            new() { tray_id = 1, plant_type = "Lettuce", target_temperature = 23.0, target_humidity = 70.0, target_light = 1000.0 }
        };

        // Function call
        var result = _plantDataService.CombineData(sensorReadings, plantConfigurations);

        // Start testing
        Assert.Empty(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No sensor data found for tray_id 1")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void CombineData_WithEmptyPlantConfigurations_ReturnsEmptyResult()
    {
        // Data setup
        var sensorReadings = new List<SensorReading>
        {
            new() { tray_id = 1, temperature = 22.5, humidity = 65.0, light = 1200.0 }
        };
        var plantConfigurations = new List<PlantConfiguration>();

        // Function call
        var result = _plantDataService.CombineData(sensorReadings, plantConfigurations);

        // Start testing
        Assert.Empty(result);
    }

    #endregion

    #region CombineData Tests - Edge Cases and Validation

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void CombineData_WithInvalidTrayIds_HandlesGracefully(int invalidTrayId)
    {
        // Data setup
        var sensorReadings = new List<SensorReading>
        {
            new() { tray_id = invalidTrayId, temperature = 22.5, humidity = 65.0, light = 1200.0 }
        };
        var plantConfigurations = new List<PlantConfiguration>
        {
            new() { tray_id = invalidTrayId, plant_type = "Test", target_temperature = 23.0, target_humidity = 70.0, target_light = 1000.0 }
        };

        // Function call
        var result = _plantDataService.CombineData(sensorReadings, plantConfigurations);

        // Start testing
        Assert.Single(result);
        Assert.Equal(invalidTrayId, result.First().TrayId);
    }

    [Fact]
    public void CombineData_WithNegativeValues_AcceptsData()
    {
        // Data setup - Test edge cases with negative sensor values
        var sensorReadings = new List<SensorReading>
        {
            new() { tray_id = 1, temperature = -5.0, humidity = -10.0, light = -100.0 }
        };
        var plantConfigurations = new List<PlantConfiguration>
        {
            new() { tray_id = 1, plant_type = "Test", target_temperature = 23.0, target_humidity = 70.0, target_light = 1000.0 }
        };

        // Function call
        var result = _plantDataService.CombineData(sensorReadings, plantConfigurations);

        // Start testing
        Assert.Single(result);
        var combined = result.First();
        Assert.Equal(-5.0, combined.CurrentTemperature);
        Assert.Equal(-10.0, combined.CurrentHumidity);
        Assert.Equal(-100.0, combined.CurrentLight);
    }

    [Fact]
    public void CombineData_WithExtremeValues_AcceptsData()
    {
        // Data setup - Test with extreme but valid values
        var sensorReadings = new List<SensorReading>
        {
            new() { tray_id = 1, temperature = 999.99, humidity = 100.0, light = 50000.0 }
        };
        var plantConfigurations = new List<PlantConfiguration>
        {
            new() { tray_id = 1, plant_type = "Test", target_temperature = 1000.0, target_humidity = 100.0, target_light = 50000.0 }
        };

        // Function call
        var result = _plantDataService.CombineData(sensorReadings, plantConfigurations);

        // Start testing
        Assert.Single(result);
        var combined = result.First();
        Assert.Equal(999.99, combined.CurrentTemperature);
        Assert.Equal(100.0, combined.CurrentHumidity);
        Assert.Equal(50000.0, combined.CurrentLight);
    }

    #endregion

    #region SaveToDatabase Tests - Success Scenarios

    [Fact]
    public async Task SaveToDatabase_NewData_InsertsSuccessfully()
    {
        // Data setup
        var combinedData = new List<CombinedPlantData>
        {
            new()
            {
                TrayId = 1,
                PlantType = "Lettuce",
                CurrentTemperature = 22.5,
                CurrentHumidity = 65.0,
                CurrentLight = 1200.0,
                TargetTemperature = 23.0,
                TargetHumidity = 70.0,
                TargetLight = 1000.0,
                Timestamp = DateTime.UtcNow
            }
        };

        // function call
        var result = await _plantDataService.SaveToDatabase(combinedData);

        // start testing
        Assert.True(result);
        var savedData = await _context.CombinedPlantData.FirstOrDefaultAsync(x => x.TrayId == 1);
        Assert.NotNull(savedData);
        Assert.Equal("Lettuce", savedData.PlantType);
        Assert.Equal(22.5, savedData.CurrentTemperature);
    }

    [Fact]
    public async Task SaveToDatabase_ExistingData_UpdatesSuccessfully()
    {
        // Data setup
        var existingData = new CombinedPlantData
        {
            TrayId = 1,
            PlantType = "OldType",
            CurrentTemperature = 20.0,
            CurrentHumidity = 60.0,
            CurrentLight = 900.0,
            TargetTemperature = 21.0,
            TargetHumidity = 65.0,
            TargetLight = 950.0,
            Timestamp = DateTime.UtcNow.AddHours(-1)
        };

        await _context.CombinedPlantData.AddAsync(existingData);
        await _context.SaveChangesAsync();

        // function call
        var updatedData = new List<CombinedPlantData>
        {
            new()
            {
                TrayId = 1,
                PlantType = "Lettuce",
                CurrentTemperature = 22.5,
                CurrentHumidity = 65.0,
                CurrentLight = 1200.0,
                TargetTemperature = 23.0,
                TargetHumidity = 70.0,
                TargetLight = 1000.0,
                Timestamp = DateTime.UtcNow
            }
        };
        var result = await _plantDataService.SaveToDatabase(updatedData);

        // start testing
        Assert.True(result);
        var savedData = await _context.CombinedPlantData.FirstOrDefaultAsync(x => x.TrayId == 1);
        Assert.NotNull(savedData);
        Assert.Equal("Lettuce", savedData.PlantType); // Should be updated
        Assert.Equal(22.5, savedData.CurrentTemperature); // Should be updated
    }

    [Fact]
    public async Task SaveToDatabase_MultipleRecords_HandlesUpsertCorrectly()
    {
        // Data setup
        var existingData = new CombinedPlantData
        {
            TrayId = 1,
            PlantType = "OldType",
            CurrentTemperature = 20.0,
            CurrentHumidity = 60.0,
            CurrentLight = 900.0,
            TargetTemperature = 21.0,
            TargetHumidity = 65.0,
            TargetLight = 950.0,
            Timestamp = DateTime.UtcNow.AddHours(-1)
        };

        await _context.CombinedPlantData.AddAsync(existingData);
        await _context.SaveChangesAsync();

        // function call
        var combinedData = new List<CombinedPlantData>
        {
            // Update existing
            new()
            {
                TrayId = 1,
                PlantType = "Lettuce",
                CurrentTemperature = 22.5,
                CurrentHumidity = 65.0,
                CurrentLight = 1200.0,
                TargetTemperature = 23.0,
                TargetHumidity = 70.0,
                TargetLight = 1000.0,
                Timestamp = DateTime.UtcNow
            },
            // Insert new
            new()
            {
                TrayId = 2,
                PlantType = "Tomato",
                CurrentTemperature = 24.0,
                CurrentHumidity = 70.0,
                TargetTemperature = 25.0,
                TargetHumidity = 65.0,
                TargetLight = 1200.0,
                Timestamp = DateTime.UtcNow
            }
        };

        var result = await _plantDataService.SaveToDatabase(combinedData);

        // Start testing
        Assert.True(result);

        var allRecords = await _context.CombinedPlantData.ToListAsync();
        Assert.Equal(2, allRecords.Count);

        var tray1 = allRecords.First(x => x.TrayId == 1);
        Assert.Equal("Lettuce", tray1.PlantType); // Updated

        var tray2 = allRecords.First(x => x.TrayId == 2);
        Assert.Equal("Tomato", tray2.PlantType); // New record
    }

    [Fact]
    public async Task SaveToDatabase_Success_ReturnsTrueAndLogsNotRequired()
    {
        // Data setup
        var combinedData = new List<CombinedPlantData>
        {
            new()
            {
                TrayId = 1,
                PlantType = "Lettuce",
                CurrentTemperature = 22.5,
                CurrentHumidity = 65.0,
                CurrentLight = 1200.0,
                TargetTemperature = 23.0,
                TargetHumidity = 70.0,
                TargetLight = 1000.0,
                Timestamp = DateTime.UtcNow
            }
        };

        // Function call
        var result = await _plantDataService.SaveToDatabase(combinedData);

        // Start testing
        Assert.True(result);

        var savedData = await _context.CombinedPlantData.FirstOrDefaultAsync(x => x.TrayId == 1);
        Assert.NotNull(savedData);
        Assert.Equal(22.5, savedData.CurrentTemperature);
    }

    [Fact]
    public async Task SaveToDatabase_WithEmptyList_ReturnsTrue()
    {
        // Data setup
        var combinedData = new List<CombinedPlantData>();

        // Function call
        var result = await _plantDataService.SaveToDatabase(combinedData);

        // Start testing
        Assert.True(result);
    }

    #endregion

    #region SaveToDatabase Tests - Failure Scenarios

    [Fact]
    public async Task SaveToDatabase_DatabaseException_ReturnsFalse()
    {
        // Data setup - Use a disposed context to simulate database error
        var disposedContext = new PlantSensorDbContext(new DbContextOptionsBuilder<PlantSensorDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options);
        disposedContext.Dispose(); // This will cause database operations to fail

        var plantDataServiceWithBadContext = new PlantDataService(disposedContext, _loggerMock.Object);
        var combinedData = new List<CombinedPlantData>
        {
            new()
            {
                TrayId = 1,
                PlantType = "Lettuce",
                CurrentTemperature = 22.5,
                CurrentHumidity = 65.0,
                CurrentLight = 1200.0,
                TargetTemperature = 23.0,
                TargetHumidity = 70.0,
                TargetLight = 1000.0,
                Timestamp = DateTime.UtcNow
            }
        };

        // Function call
        var result = await plantDataServiceWithBadContext.SaveToDatabase(combinedData);

        // Start testing
        Assert.False(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("error")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}
