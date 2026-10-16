using System.ComponentModel.DataAnnotations;

namespace RaceDay.DTOs.API.DTOs;

public class RegistDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required, RegularExpression("^(Organiser|Participant)$")]
    public string Role { get; set; } = string.Empty;

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }

    public string? Name { get; set; }
    public string? OrganizationName { get; set; }
}

public class LoginDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class UpdateProfileDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }

    public string? Name { get; set; }
    public string? OrganizationName { get; set; }
}

public class EventDto
{
    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required, StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [Required]
    public DateTime EventDate { get; set; }

    [Required, RegularExpression("^(Run|Walk|Cycle)$")]
    public string EventType { get; set; } = "Run";

    public string? RouteDetails { get; set; }
    public string? WeatherInfo { get; set; }
}

public class CategoryDto
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "1000.00")]
    public decimal Distance { get; set; }

    [Range(1, 120)]
    public int? AgeRestriction { get; set; }
}

public class RegistrationDto
{
    [Required, Range(1, int.MaxValue)]
    public int CategoryID { get; set; }
}

public class ResultDto
{
    [Required, Range(1, int.MaxValue)]
    public int RegistrationID { get; set; }

    [Required]
    public TimeSpan FinishTime { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int Position { get; set; }

    [StringLength(500)]
    public string? SplitTimes { get; set; }
}
