using ApiClient.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading.Tasks;

public class ApiController : Controller
{
    private readonly ServiceStatusProvider _serviceStatusProvider;
    private readonly HttpClient _httpClient;
    private readonly ILogger<HomeController> _logger;

    public ApiController(ServiceStatusProvider serviceStatusProvider,  HttpClient httpClient, ILogger<HomeController> logger)
    {
        _serviceStatusProvider = serviceStatusProvider;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var serviceStatus = new Dictionary<string, string>();

        ViewData["ServiceStatus"] = await _serviceStatusProvider.GetServiceStatus(_httpClient);
        return View();
    }

    #region old upload methode using post on the same page
    //[HttpPost]
    //public async Task<IActionResult> Upload(IFormFile file)
    //{
    //    if (file == null || file.Length == 0)
    //    {
    //        return BadRequest("No file uploaded.");
    //    }

    //    if(_serviceStatusProvider.currentStatus == null)
    //    {
    //        await _serviceStatusProvider.GetServiceStatus(_httpClient);
    //    }

    //    using var content = new MultipartFormDataContent();
    //    using var fileStream = file.OpenReadStream();
    //    var fileContent = new StreamContent(fileStream);
    //    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
    //    content.Add(fileContent, "image", file.FileName);

    //    var response = await _httpClient.PostAsync("http://localhost:5000/yoloDetection/detect", content);
    //    if (response.IsSuccessStatusCode)
    //    {
    //        var result = await response.Content.ReadAsStringAsync();

    //        ViewData["ServiceStatus"] = _serviceStatusProvider.currentStatus!;

    //        return View("Index");
    //    }

    //    return BadRequest("Failed to upload file.");
    //}

    #endregion
}