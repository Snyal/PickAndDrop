namespace RobotManagementSystem.Models
{
    public class DetectionResult
    {
        public required string ObjectName { get; set; }
        public required float Confidence { get; set; }
        public required double X { get; set; }
        public required double Y { get; set; }
        public required double Width { get; set; }
        public required double Height { get; set; }
    }
}