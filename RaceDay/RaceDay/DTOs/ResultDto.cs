using System.ComponentModel.DataAnnotations;

namespace RaceDay.DTOs.API.DTOs;

    public class ResultDto
{
    [Range(1, int.MaxValue)]
    public int RegistrationID { get; set; }

    public TimeSpan FinishTime { get; set; }

    [Range(1, int.MaxValue)]
    public int Position { get; set; }

    public string? SplitTimes { get; set; }
}

