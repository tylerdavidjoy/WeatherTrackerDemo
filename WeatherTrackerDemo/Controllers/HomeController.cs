using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using WeatherTrackerDemo.Models;
using WeatherTrackerDemo.Repositories;

namespace WeatherTrackerDemo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private IConfiguration _configuration;
        private readonly IWeatherRepository _weatherRepository;
        public HomeController(ILogger<HomeController> logger, IConfiguration configuration, IWeatherRepository weatherRepository)
        {
            _logger = logger;
            _configuration = configuration;
            _weatherRepository = weatherRepository;
        }

        public IActionResult Index()
        {
            Task.Run(() => _weatherRepository.WakeDatabase());
            return View(new WeatherModel());
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        [HttpPost]
        public async Task<IActionResult> GetWeather(WeatherModel model)
        {
            var locationKey = await _weatherRepository.GetLocationKey(model.Location);
            await model.GetWeather(_configuration.GetValue<string>("WEATHER-API-KEY"), locationKey);
            return View("index", model);
        }
    }
}
