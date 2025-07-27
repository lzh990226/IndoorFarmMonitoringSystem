using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using PlantSensorDataAPI.Configuration;
using PlantSensorDataAPI.Models;
using PlantSensorDataAPI.Services;
using System.Net;
using System.Text.Json;

namespace PlantSensorDataAPI.Tests.Services;

public class ExternalApiServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<ILogger<ExternalApiService>> _loggerMock;
    private readonly Mock<IOptions<ExternalApiSettings>> _optionsMock;
    private readonly ExternalApiSettings _apiSettings;
    private readonly HttpClient _httpClient;
    private readonly ExternalApiService _externalApiService;

    public ExternalApiServiceTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _loggerMock = new Mock<ILogger<ExternalApiService>>();
        _optionsMock = new Mock<IOptions<ExternalApiSettings>>();

        _apiSettings = new ExternalApiSettings
        {
            SensorReadingsUrl = "http://test.com/sensor-readings",
            PlantConfigurationsUrl = "http://test.com/plant-configurations"
        };

        _optionsMock.Setup(x => x.Value).Returns(_apiSettings);
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _externalApiService = new ExternalApiService(_httpClient, _optionsMock.Object, _loggerMock.Object);
    }

    #region GetSensorReadingsAsync Tests - Success Scenarios

    [Fact]
    public async Task GetSensorReadingsAsync_Success_ReturnsData()
    {
        // Mock response
        var sensorReadings = new List<SensorReading>
        {
            new() { tray_id = 1, temperature = 22.5, humidity = 65.0, light = 1200.0 },
            new() { tray_id = 2, temperature = 24.0, humidity = 70.0, light = 1100.0 }
        };
        var jsonResponse = JsonSerializer.Serialize(sensorReadings);

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // function call
        var result = await _externalApiService.GetSensorReadingsAsync();

        // start testing
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].tray_id);
        Assert.Equal(22.5, result[0].temperature);
        Assert.Equal(65.0, result[0].humidity);
        Assert.Equal(1200.0, result[0].light);
        Assert.Equal(2, result[1].tray_id);
        Assert.Equal(24.0, result[1].temperature);
        Assert.Equal(70.0, result[1].humidity);
        Assert.Equal(1100.0, result[1].light);
    }

    #endregion

    #region GetSensorReadingsAsync Tests - Failure Scenarios

    [Fact]
    public async Task GetSensorReadingsAsync_HttpRequestException_ReturnsNull()
    {
        // Mock error
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // function call
        var result = await _externalApiService.GetSensorReadingsAsync();

        // start testing
        Assert.Null(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("HTTP error")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetSensorReadingsAsync_InvalidJson_ReturnsNull()
    {
        // Mock response with invalid JSON
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("invalid json")
            });

        // Function call
        var result = await _externalApiService.GetSensorReadingsAsync();

        // Start testing
        Assert.Null(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("JSON parsing error")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region GetPlantConfigurationsAsync Tests - Success Scenarios

    [Fact]
    public async Task GetPlantConfigurationsAsync_Success_ReturnsData()
    {
        // Arrange
        var plantConfigurations = new List<PlantConfiguration>
        {
            new() { tray_id = 1, plant_type = "Lettuce", target_temperature = 23.0, target_humidity = 70.0, target_light = 1000.0 },
            new() { tray_id = 2, plant_type = "Tomato", target_temperature = 25.0, target_humidity = 65.0, target_light = 1200.0 }
        };
        var jsonResponse = JsonSerializer.Serialize(plantConfigurations);

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _externalApiService.GetPlantConfigurationsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].tray_id);
        Assert.Equal("Lettuce", result[0].plant_type);
        Assert.Equal(23.0, result[0].target_temperature);
        Assert.Equal(70.0, result[0].target_humidity);
        Assert.Equal(1000.0, result[0].target_light);
        Assert.Equal(2, result[1].tray_id);
        Assert.Equal("Tomato", result[1].plant_type);
        Assert.Equal(25.0, result[1].target_temperature);
        Assert.Equal(65.0, result[1].target_humidity);
        Assert.Equal(1200.0, result[1].target_light);
    }

    #endregion

    #region GetPlantConfigurationsAsync Tests - Failure Scenarios

    [Fact]
    public async Task GetPlantConfigurationsAsync_HttpRequestException_ReturnsNull()
    {
        // Mock error
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // function call
        var result = await _externalApiService.GetPlantConfigurationsAsync();

        // Start testing
        Assert.Null(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("HTTP error")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetSensorReadingsAsync_EmptyResponse_ReturnsEmptyList()
    {
        // Arrange
        var jsonResponse = "[]";

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _externalApiService.GetSensorReadingsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPlantConfigurationsAsync_EmptyResponse_ReturnsEmptyList()
    {
        // Arrange
        var jsonResponse = "[]";

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _externalApiService.GetPlantConfigurationsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetSensorReadingsAsync_TimeoutException_ReturnsNull()
    {
        // Arrange
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timeout", new TimeoutException()));

        // Act
        var result = await _externalApiService.GetSensorReadingsAsync();

        // Assert
        Assert.Null(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Timeout error")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPlantConfigurationsAsync_TimeoutException_ReturnsNull()
    {
        // Arrange
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timeout", new TimeoutException()));

        // Act
        var result = await _externalApiService.GetPlantConfigurationsAsync();

        // Assert
        Assert.Null(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Timeout error")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPlantConfigurationsAsync_InvalidJson_ReturnsNull()
    {
        // Arrange
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("invalid json")
            });

        // Act
        var result = await _externalApiService.GetPlantConfigurationsAsync();

        // Assert
        Assert.Null(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("JSON parsing error")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetSensorReadingsAsync_UnexpectedException_ReturnsNull()
    {
        // Arrange
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        // Act
        var result = await _externalApiService.GetSensorReadingsAsync();

        // Assert
        Assert.Null(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unexpected error")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}
