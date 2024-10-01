using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RobotMicroservice.Services;

[ApiController]
[Route("[controller]")]
public class RobotController : ControllerBase
{
    private readonly KinematicService _kinematicService;
    private readonly List<Dictionary<String, dynamic>> robots;

    public RobotController(KinematicService kinematicService)
    {
        _kinematicService = kinematicService;
        int[][] angleFreedom = new int[][] {
            [0,  1,  0],
            [0, 0,  1],
            [0,  0,  1],
            [1,  0,  0],
            [0, 0, 1],
            [1, 0,  0],
        };

        Dictionary<String, dynamic> robot1 = new Dictionary<string, dynamic>();
        double[][] configuration = new double[][] {
            [1,  1,  0],
            [0, 10,  0],
            [5,  0,  0],
            [3,  0,  0],
            [0, -2,  0],
        };

        robot1.Add("name", "robot1");
        robot1.Add("configuration", configuration);
        robot1.Add("angleFreedom", angleFreedom);

        Dictionary<String, dynamic> robot2 = new Dictionary<string, dynamic>();
        double[][] configuration2 = new double[][] {
            [1,  4,  0],
            [0, 6,  0],
            [6,  2,  0],
            [2,  0,  0],
            [0, -2,  0],
        };

        robot2.Add("name", "robot2");
        robot2.Add("configuration", configuration2);
        robot2.Add("angleFreedom", angleFreedom);

        robots = [robot1, robot2];
  
    }

    [HttpGet]
    public IActionResult GetRobot()
    {
        return Ok(robots);
    }

    [HttpPost("initRobot")]
    public ActionResult<String> InitRobot([FromBody] string nameRobot)
    {
        double[][]? selectedConfiguration = null;
        foreach (Dictionary<string, dynamic> robot in robots)
        {
            if (robot.TryGetValue("name", out var name) && name == nameRobot)
            {
                selectedConfiguration = robot["configuration"];
            }
        }

        if(selectedConfiguration == null)
        {
            return BadRequest("Name robot not found");
        }

        _kinematicService.SetRobotConfiguration(selectedConfiguration);
        return Ok($"{nameRobot} is now load");
    }

    [HttpPost("moveTo")]
    public ActionResult<double[]> MoveTo([FromBody] Dictionary<string, double> parameters)
    {
        Console.WriteLine($"x : {parameters["x"]}, y : {parameters["y"]}, z: {parameters["z"]}");

        double x = parameters["x"];
        double y = parameters["y"];
        double z = parameters["z"];
        double a = parameters["a"];
        double b = parameters["b"];
        double c = parameters["c"];

        double[] angles = _kinematicService.inverse(x,y, z, a, b, c);

        // TODO : Check if location is reachable
        return Ok(angles);
    }

}