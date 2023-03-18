using ActorWebDemo.Service;
using Microsoft.AspNetCore.Mvc;

namespace ActorWebDemo.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot",
        "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;
    private readonly Backend _backend;

    public WeatherForecastController(ILogger<WeatherForecastController> logger, Backend backend)
    {
        _logger = logger;
        _backend = backend;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
    }

    [HttpGet, Route("dosomething")]
    public IActionResult Backend()
    {
        _backend.DoSomething();
        return Ok();
    }

    [HttpGet, Route("echo/{message}")]
    public async Task<IActionResult> Echo(string message)
    {
        var echo = await _backend.Echo(message);
        return Ok(echo);
    }
}