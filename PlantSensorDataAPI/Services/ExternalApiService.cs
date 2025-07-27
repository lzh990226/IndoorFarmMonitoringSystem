using Microsoft.Extensions.Options;
using PlantSensorDataAPI.Configuration;
using PlantSensorDataAPI.Models;
using System.Collections;
using System.Diagnostics;
using System.Text.Json;

namespace PlantSensorDataAPI.Services;

public class ExternalApiService : IExternalApiService
{
    private readonly HttpClient _httpClient;
    private readonly ExternalApiSettings _apiSettings;
    private readonly ILogger<ExternalApiService> _logger;

    public ExternalApiService(HttpClient httpClient, IOptions<ExternalApiSettings> apiSettings, ILogger<ExternalApiService> logger)
    {
        _httpClient = httpClient;
        _apiSettings = apiSettings.Value;
        _logger = logger;
    }

    public async Task<List<SensorReading>?> GetSensorReadingsAsync()
    {
        return await ExecuteApiCallAsync<List<SensorReading>>(_apiSettings.SensorReadingsUrl);
    }

    public async Task<List<PlantConfiguration>?> GetPlantConfigurationsAsync()
    {
        return await ExecuteApiCallAsync<List<PlantConfiguration>>(_apiSettings.PlantConfigurationsUrl);
    }

    private async Task<T?> ExecuteApiCallAsync<T>(string url) where T : class
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Starting API call to retrieve data from {Url}", url);

        try
        {
            var response = await _httpClient.GetStringAsync(url);
            stopwatch.Stop();

            _logger.LogInformation("Successfully received response in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

            var deserializedData = JsonSerializer.Deserialize<T>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var count = deserializedData is ICollection collection ? collection.Count : 1;
            return deserializedData;
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "HTTP error retrieving data from {Url} after {ElapsedMs}ms. Status: {StatusCode}",
                url, stopwatch.ElapsedMilliseconds, ex.Data["StatusCode"]);
            return null;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Timeout error retrieving data from {Url} after {ElapsedMs}ms",
                url, stopwatch.ElapsedMilliseconds);
            return null;
        }
        catch (JsonException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "JSON parsing error for response after {ElapsedMs}ms. Response may be malformed.",
                stopwatch.ElapsedMilliseconds);
            return null;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Unexpected error retrieving data after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            return null;
        }
    }
}
