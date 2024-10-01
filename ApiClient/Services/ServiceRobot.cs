using ApiClient.Models;
using Newtonsoft.Json;

namespace ApiClient.Services
{
    public class ServiceRobot
    {
        public async Task<List<RobotModel>?> GetRobots(HttpClient httpClient)
        {
            List<RobotModel>? robots = null;
            try
            {
                // send a GET request to check the service
                var response = await httpClient.GetAsync("http://localhost:5000/robot");

                // succed (Code HTTP 200)
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    robots = JsonConvert.DeserializeObject<List<RobotModel>>(responseBody);
                     
                    if(robots == null)
                    {
                        return robots;
                    }

                    for(int i = 0; i< robots.Count(); i++) {
                        for (int y = 0; y < robots[i].AngleFreedom[0].Count(); y++)
                        {
                            Console.WriteLine(robots[i].AngleFreedom[0][y]);
                        }
                    }

                    return robots;
                }
                return robots;
            }
            catch (Exception _) {
                return robots;
            } 
        }
    }
}
