using Moq;
using Moq.Protected;
using System.Net;
using TestTasks.WeatherFromAPI;
using TestTasks.WeatherFromAPI.Models;

namespace TestProject.Tests
{
    public class WeatherManagerTests
    {
        private readonly Mock<HttpMessageHandler> _mockHandler;
        private readonly HttpClient _mockHttpClient;
        private readonly WeatherManager _weatherManager;
        private const string ApiKey = "testApiKey";

        public WeatherManagerTests()
        {
            _mockHandler = new Mock<HttpMessageHandler>();
            _mockHttpClient = new HttpClient(_mockHandler.Object);
            _weatherManager = new WeatherManager(_mockHttpClient, ApiKey);
        }

        [Fact]
        public void Constructor_ShouldInitializeProperly()
        {
            var manager = new WeatherManager(_mockHttpClient, ApiKey);

            Assert.NotNull(manager);
        }

        [Fact]
        public async Task CompareWeather_ShouldThrowException_WhenDayCountIsInvalid()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _weatherManager.CompareWeather("London", "Paris", 0));
            await Assert.ThrowsAsync<ArgumentException>(() => _weatherManager.CompareWeather("London", "Paris", 6));
        }

        [Fact]
        public async Task CompareWeather_ShouldThrowException_WhenCityNotFound()
        {
            _mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<System.Threading.CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

            await Assert.ThrowsAsync<ArgumentException>(() => _weatherManager.CompareWeather("NonExistentCity", "Paris", 3));
        }


        [Fact]
        public async Task GetCityLocation_ShouldReturnLocation_WhenFound()
        {
            var cityName = "London";
            var expectedLocation = new GeoData(51.5074f, -0.1278f);

            var validResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[{\"lat\": 51.5074, \"lon\": -0.1278}]")
            };

            _mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<System.Threading.CancellationToken>())
                .ReturnsAsync(validResponse);

            var location = await _weatherManager.GetCityLocation(cityName);

            Assert.NotNull(location);
            Assert.Equal(expectedLocation.lat, location.lat);
            Assert.Equal(expectedLocation.lon, location.lon);
        }

        [Fact]
        public async Task GetCityLocation_ShouldReturnNull_WhenCityNotFound()
        {
            var validResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]")
            };

            _mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<System.Threading.CancellationToken>())
                .ReturnsAsync(validResponse);

            var location = await _weatherManager.GetCityLocation("NonExistentCity");

            Assert.Null(location);
        }

        [Fact]
        public async Task GetDailyWeatherData_ShouldReturnData_WhenValid()
        {
            var location = new GeoData(51.5074f, -0.1278f);
            var dayCount = 3;

            var validResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\": [{\"temp\": 15.0, \"rain\": {\"oneh\": 0.2}}]}")
            };

            _mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<System.Threading.CancellationToken>())
                .ReturnsAsync(validResponse);

            var dailyData = await _weatherManager.GetDailyWeatherData(location, dayCount);

            Assert.NotEmpty(dailyData);
            Assert.Contains(dailyData, entry => entry.Value.avgTemp == 15.0f && entry.Value.totalRain == 0.2f);
        }

        [Fact]
        public void CalculateComparison_ShouldReturnCorrectComparisonResult()
        {
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

            var result = _weatherManager.CalculateComparison(dailyA, dailyB, cityA, cityB);

            Assert.Equal(1, result.WarmerDaysCount);
            Assert.Equal(0, result.RainierDaysCount);
        }
    }

}