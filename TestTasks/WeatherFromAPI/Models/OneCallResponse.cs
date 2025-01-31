using System.Collections.Generic;

namespace TestTasks.WeatherFromAPI.Models
{
    public record OneCallResponse
    {
        public List<RespData> data { get; set; } = new List<RespData>();
    }
}
