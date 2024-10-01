using ApiClient.Controllers;
using ApiClient.Models;
using ApiClient.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;

namespace ApiClient.RobotController;

public class RobotController : Controller
{
    private readonly ServiceRobot _serviceRobot;
    private readonly HttpClient _httpClient;
    
    public RobotController(ServiceRobot serviceRobot, HttpClient httpClient)
    {
        _serviceRobot = serviceRobot;
        _httpClient = httpClient;
    }

    public async Task<IActionResult> Index()
    {
        List<RobotModel>? robotsAvailable = await _serviceRobot.GetRobots(_httpClient);


        ViewData["robotsAvailable"] = (robotsAvailable != null)? robotsAvailable! : [];

        return View();
    }

}
