using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using TestTasks.WeatherFromAPI.Models;

namespace TestTasks.WeatherFromAPI
{
    public class WeatherManager
    {
        private readonly string _APIKey;
        private readonly HttpClient _client;

        public WeatherManager(HttpClient client, string apiKey)
        {
            _client = client;
            _APIKey = apiKey;
        }

        public async Task<WeatherComparisonResult> CompareWeather(string cityA, string cityB, int dayCount)
        {
            if (dayCount < 1 || dayCount > 5)
            {
                throw new ArgumentException("dayCount must be between 1 and 5.");
            }

            GeoData locationA = await GetCityLocation(cityA);
            GeoData locationB = await GetCityLocation(cityB);

            if (locationA == null || locationB == null)
            {
                throw new ArgumentException("One or both cities not found.");
            }

            Dictionary<DateTime, (float avgTemp, float totalRain)> dailyA = await GetDailyWeatherData(locationA, dayCount);
            Dictionary<DateTime, (float avgTemp, float totalRain)> dailyB = await GetDailyWeatherData(locationB, dayCount);

            return CalculateComparison(dailyA, dailyB, cityA, cityB);
        }

        public async Task<GeoData> GetCityLocation(string cityName)
        {
            string url = $"http://api.openweathermap.org/geo/1.0/direct?q={cityName}&limit=1&appid={_APIKey}";
            HttpResponseMessage response = await _client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            string jsonResponse = await response.Content.ReadAsStringAsync();
            var locations = JsonSerializer.Deserialize<List<GeoData>>(jsonResponse);

            return locations?.Count > 0 ? locations[0] : null;
        }

        public async Task<Dictionary<DateTime, (float avgTemp, float totalRain)>> GetDailyWeatherData(GeoData location, int dayCount)
        {
            var dailyWeather = new Dictionary<DateTime, (float avgTemp, float totalRain)>();

            for (int i = dayCount; i > 0; i--)
            {
                DateTime targetDate = DateTime.UtcNow.AddDays(-i);
                long unixTimestamp = new DateTimeOffset(targetDate).ToUnixTimeSeconds();
                string url = $"https://api.openweathermap.org/data/3.0/onecall/timemachine?lat={location.lat}&lon={location.lon}&dt={unixTimestamp}&appid={_APIKey}&units=metric";

                HttpResponseMessage response = await _client.GetAsync(url);
                if (!response.IsSuccessStatusCode) continue;

                string jsonResponse = await response.Content.ReadAsStringAsync();
                var weatherResponse = JsonSerializer.Deserialize<OneCallResponse>(jsonResponse);

                if (weatherResponse?.data != null && weatherResponse.data.Count > 0)
                {
                    var weatherData = weatherResponse.data[0];

                    dailyWeather[targetDate.Date] = (weatherData.temp, weatherData.rain.oneh);
                }
            }

            return dailyWeather;
        }

        public WeatherComparisonResult CalculateComparison(
            Dictionary<DateTime, (float avgTemp, float totalRain)> dailyA,
            Dictionary<DateTime, (float avgTemp, float totalRain)> dailyB,
            string cityA, string cityB)
        {
            int warmerDays = 0, rainierDays = 0;

            foreach (var date in dailyA.Keys.Intersect(dailyB.Keys))
            {
                var (avgTempA, totalRainA) = dailyA[date];
                var (avgTempB, totalRainB) = dailyB[date];

                if (avgTempA > avgTempB) warmerDays++;
                if (totalRainA > totalRainB) rainierDays++;
            }

            return new WeatherComparisonResult(cityA, cityB, warmerDays, rainierDays);
        }
    }
}
