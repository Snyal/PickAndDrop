using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RobotManagementSystem.Models;
using RobotManagementSystem.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
namespace RobotManagementSystem.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class YoloDetectionController : ControllerBase
    {
        private readonly YoloService _yoloService;

        public YoloDetectionController(YoloService yoloService)
        {
            _yoloService = yoloService;
        }

        [HttpGet]
        public IActionResult GetModelInformation()
        {
            return Ok(_yoloService.GetModelInformation());
        }

        [HttpPost("detect")]
        public ActionResult<List<DetectionResult>> DetectObjects(IFormFile image)
        {
            Console.WriteLine("Starting detection");
            if (image == null || image.Length == 0)
            {
                return BadRequest("No image provided.");
            }

            using var stream = image.OpenReadStream();
            var results =  _yoloService.DetectObjects(stream);

            return Ok(results);
        }

        [HttpPost("initModel")]
        public ActionResult<String> InitModel([FromBody] string modelName)
        {

            string[] modelsPath = Directory.GetFiles("Yolov8");
            string[] nameModels = new string[modelsPath.Count()];

            for (int i = 0; i<modelsPath.Count(); i++ )
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(modelsPath[i]);
                nameModels[i] = fileNameWithoutExtension;
            }

            if (!nameModels.Contains(modelName))
            {
                return BadRequest($"no modele with this name ({modelName}) was found");
            }

            _yoloService.SetInferenceSession(modelName);
            return Ok("${nameModel} is now load");
           
       
        }
    }
}