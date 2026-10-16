using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Models.API.Models;

public class Organiser
{
    [Key]
    public int OrganizerID { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(150)]
    public string ContactInfo { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string OrganizationName { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public ICollection<EventOrganiser> EventOrganisers { get; set; } = new List<EventOrganiser>();
}

public class Participant
{
    [Key]
    public int ParticipantID { get; set; }

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Gender { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime DateOfBirth { get; set; }

    [Required, EmailAddress, MaxLength(150)]
    public string ContactInfo { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
}

public class Event
{
    [Key]
    public int EventID { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    public DateTime EventDate { get; set; }

    [Required, MaxLength(50)]
    public string EventType { get; set; } = "Run";

    [MaxLength(1000)]
    public string RouteDetails { get; set; } = string.Empty;

    [MaxLength(500)]
    public string WeatherInfo { get; set; } = string.Empty;

    public ICollection<EventOrganiser> EventOrganisers { get; set; } = new List<EventOrganiser>();
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}

public class EventOrganiser
{
    public int EventID { get; set; }
    public Event Event { get; set; } = null!;

    public int OrganizerID { get; set; }
    public Organiser Organiser { get; set; } = null!;
}

public class Category
{
    [Key]
    public int CategoryID { get; set; }

    public int EventID { get; set; }
    public Event Event { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "decimal(8,2)")]
    public decimal Distance { get; set; }

    public int? AgeRestriction { get; set; }

    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
}

public class Registration
{
    [Key]
    public int RegistrationID { get; set; }

    public int ParticipantID { get; set; }
    public Participant Participant { get; set; } = null!;

    public int CategoryID { get; set; }
    public Category Category { get; set; } = null!;

    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

    [Required, MaxLength(30)]
    public string PaymentStatus { get; set; } = "Pending";

    public Result? Result { get; set; }
}

public class Result
{
    [Key]
    public int ResultID { get; set; }

    public int RegistrationID { get; set; }
    public Registration Registration { get; set; } = null!;

    public TimeSpan? FinishTime { get; set; }

    public int? Position { get; set; }

    [MaxLength(500)]
    public string? SplitTimes { get; set; }
}
