using System.ComponentModel.DataAnnotations;

namespace RaceDay.DTOs.API.DTOs;

    public class RegistrationDto
{
    [Range(1, int.MaxValue)]
    public int CategoryID { get; set; }
    public string? FirstName { get; internal set; }
    public string? LastName { get; internal set; }
    public object DateOfBirth { get; internal set; }
    public object Gender { get; internal set; }
}


