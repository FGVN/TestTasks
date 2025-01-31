using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using TestTasks.WeatherFromAPI;
using TestTasks.WeatherFromAPI.Models;
using Xunit;

public class WeatherManagerTests
{
    private readonly Mock<HttpClient> _mockHttpClient;
    private readonly WeatherManager _weatherManager;
    private const string ApiKey = "testApiKey";

    public WeatherManagerTests()
    {
        _mockHttpClient = new Mock<HttpClient>();
        _weatherManager = new WeatherManager(_mockHttpClient.Object, ApiKey);
    }

    [Fact]
    public void Constructor_ShouldInitializeProperly()
    {
        // Arrange & Act
        var manager = new WeatherManager(_mockHttpClient.Object, ApiKey);

        // Assert
        Assert.NotNull(manager);
    }

    [Fact]
    public async Task CompareWeather_ShouldThrowException_WhenDayCountIsInvalid()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _weatherManager.CompareWeather("London", "Paris", 0));
        await Assert.ThrowsAsync<ArgumentException>(() => _weatherManager.CompareWeather("London", "Paris", 6));
    }

    [Fact]
    public async Task CompareWeather_ShouldThrowException_WhenCityNotFound()
    {
        // Arrange
        _mockHttpClient.Setup(client => client.GetAsync(It.IsAny<string>())).ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _weatherManager.CompareWeather("NonExistentCity", "Paris", 3));
    }

    [Fact]
    public async Task CompareWeather_ShouldReturnComparisonResult_WhenValid()
    {
        // Arrange
        var cityA = "London";
        var cityB = "Paris";
        var dayCount = 3;

        _mockHttpClient.Setup(client => client.GetAsync(It.IsAny<string>())).ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK));

        // Act
        var result = await _weatherManager.CompareWeather(cityA, cityB, dayCount);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(cityA, result.CityA);
        Assert.Equal(cityB, result.CityB);
    }

    [Fact]
    public async Task GetCityLocation_ShouldReturnLocation_WhenFound()
    {
        // Arrange
        var cityName = "London";
        var expectedLocation = new GeoData { lat = 51.5074f, lon = -0.1278f };

        _mockHttpClient.Setup(client => client.GetAsync(It.IsAny<string>())).ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("[{\"lat\": 51.5074, \"lon\": -0.1278}]")
        });

        // Act
        var location = await _weatherManager.GetCityLocation(cityName);

        // Assert
        Assert.NotNull(location);
        Assert.Equal(expectedLocation.lat, location.lat);
        Assert.Equal(expectedLocation.lon, location.lon);
    }

    [Fact]
    public async Task GetCityLocation_ShouldReturnNull_WhenCityNotFound()
    {
        // Arrange
        _mockHttpClient.Setup(client => client.GetAsync(It.IsAny<string>())).ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("[]")
        });

        // Act
        var location = await _weatherManager.GetCityLocation("NonExistentCity");

        // Assert
        Assert.Null(location);
    }

    [Fact]
    public async Task GetDailyWeatherData_ShouldReturnData_WhenValid()
    {
        // Arrange
        var location = new GeoData { lat = 51.5074f, lon = -0.1278f };
        var dayCount = 3;

        _mockHttpClient.Setup(client => client.GetAsync(It.IsAny<string>())).ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{\"data\": [{\"temp\": 15.0, \"rain\": {\"oneh\": 0.2}}]}")
        });

        // Act
        var dailyData = await _weatherManager.GetDailyWeatherData(location, dayCount);

        // Assert
        Assert.NotEmpty(dailyData);
        Assert.Contains(dailyData, entry => entry.Value.avgTemp == 15.0f && entry.Value.totalRain == 0.2f);
    }

    [Fact]
    public void CalculateComparison_ShouldReturnCorrectComparisonResult()
    {
        // Arrange
        var dailyA = new Dictionary<DateTime, (float avgTemp, float totalRain)>
        {
            { DateTime.UtcNow.Date, (15.0f, 0.2f) }
        };
        var dailyB = new Dictionary<DateTime, (float avgTemp, float totalRain)>
        {
            { DateTime.UtcNow.Date, (14.0f, 0.3f) }
        };
        var cityA = "London";
        var cityB = "Paris";

        // Act
        var result = _weatherManager.CalculateComparison(dailyA, dailyB, cityA, cityB);

        // Assert
        Assert.Equal(1, result.WarmerDays);
        Assert.Equal(0, result.RainierDays);
    }
}
