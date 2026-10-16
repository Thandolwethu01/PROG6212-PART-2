using System.ComponentModel.DataAnnotations;

namespace RaceDay.DTOs.API.DTOs;

    public class EventDtos
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = "";

    [MaxLength(1000)]
    public string Description { get; set; } = "";

    [Required]
    public string Location { get; set; } = "";

    public DateTime EventDate { get; set; }

    [Required]
    public string EventType { get; set; } = "Run";

    public string RouteDetails { get; set; } = "";
    public string WeatherInfo { get; set; } = "";
}

