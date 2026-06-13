namespace API_WateringDashboard.Models;

public class Reading
{
    public required DateTime Timestamp {get; set; }
    public required string Origin {get; set; }
    public required double? AirTemperature {get; set; }
    public required double? AirHumidity {get; set; }
    public required double? SoilHumidity {get; set; }
    public required string? Status {get; set; }
}
