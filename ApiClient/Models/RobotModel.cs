namespace ApiClient.Models
{
    public class RobotModel
    {
        public required string Name { get; set; }
        public required double[][] Configuration { get; set; }

        public required int[][] AngleFreedom { get; set; }
    }
}
