using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using TestTasks.InternationalTradeTask;
using TestTasks.VowelCounting;
using TestTasks.WeatherFromAPI;

namespace TestTasks
{
    class Program
    {
        static async Task Main()
        {
            //Below are examples of usage. However, it is not guaranteed that your implementation will be tested on those examples.            
            var stringProcessor = new StringProcessor();
            string str = File.ReadAllText("CharCounting/StringExample.txt");
            var charCount = stringProcessor.GetCharCount(str, new char[] { 'l', 'r', 'm', '1' });
            foreach (var symb in charCount)
            {
                Console.WriteLine(symb.symbol + " has been found " + symb.count + " times");
            }


            var commodityRepository = new CommodityRepository();
            Console.WriteLine(commodityRepository.GetImportTariff("Refined sugar & other prod.of refining,no syrup"));
            Console.WriteLine(commodityRepository.GetExportTariff("Iron/steel scrap not sorted or graded"));

            HttpClient client = new HttpClient();
            string APIKey = "594b8a2364cb92dd15e277824ee6c3ce";
            var weatherManager = new WeatherManager(client, APIKey);
            var comparisonResult = await weatherManager.CompareWeather("lviv,ua", "kyiv,ua",  10);

            Console.WriteLine($"Warmer days in total: {comparisonResult.WarmerDaysCount}\nRainier days in total: {comparisonResult.RainierDaysCount}");
        }
    }
}
