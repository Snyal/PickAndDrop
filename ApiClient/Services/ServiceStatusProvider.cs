
public class ServiceStatusProvider
{
    public Dictionary<string, string>? currentStatus;

    public async Task<Dictionary<string, string>> GetServiceStatus(HttpClient httpClient)
    {
        string robotsServiceStatus = await CheckServiceStatus(httpClient, "http://localhost:5000/robot");
        string yoloServiceStatus = await CheckServiceStatus(httpClient, "http://localhost:5000/yoloDetection");

        currentStatus = new Dictionary<string, string>
        {
            { "Robots", robotsServiceStatus},
            { "Yolo", yoloServiceStatus}
        };

        return currentStatus;
    }

    private async Task<string> CheckServiceStatus(HttpClient httpClient, string url)
    {
        try
        {
            // send a GET request to check the service
            var response = await httpClient.GetAsync(url);

            // succed (Code HTTP 200)
            if (response.IsSuccessStatusCode)
            {
                return "🟢 Online";
            }
            return "🔴 Offline";
        }
        catch
        {
            return "🔴 Offline";
        }
    }
}