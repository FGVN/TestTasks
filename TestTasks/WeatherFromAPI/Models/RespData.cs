namespace TestTasks.WeatherFromAPI.Models
{
    public record RespData
    {
        public long dt { get; set; }
        public float temp { get; set; }
        public RainData rain { get; set; } = new RainData();
    }
}
