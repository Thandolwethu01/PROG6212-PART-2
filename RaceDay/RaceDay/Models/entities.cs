using System.ComponentModel.DataAnnotations;


namespace RaceDay.Models.API.Models;

public class Organiser
{
    public int OrganizerID { get; set; }

    public string Name { get; set; } = "";

    public string ContactInfo { get; set; } = "";

    public string OrganizationName { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    public ICollection<EventOrganiser> EventOrganisers { get; set; }
        = new List<EventOrganiser>();
}


// This class stores the personal details of each participant.
public class Participant
{
    // This uniquely identifies each participant.
    public int ParticipantID { get; set; }

    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Gender { get; set; } = "";

    // This helps calculate the participant's age.
    public DateTime DateOfBirth { get; set; }

    // This stores the participant's contact email.
    public string ContactInfo { get; set; } = "";

    // The password is hashed before it is saved to the database.
    public string PasswordHash { get; set; } = "";

    // One participant can have multiple event registrations.
    public ICollection<Registration> Registrations { get; set; }
        = new List<Registration>();
}

// This class stores information about running, walking and cycling events.
public class Event
{
    // This is the unique ID for an event.
    public int EventID { get; set; }

    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Location { get; set; } = "";
    public DateTime EventDate { get; set; }

    // The event type can be Run, Walk or Cycle.
    public string EventType { get; set; } = "Run";

    // These fields store extra information about the event route and weather.
    public string RouteDetails { get; set; } = "";
    public string WeatherInfo { get; set; } = "";

    // An event can have multiple organisers and categories.
    public ICollection<EventOrganiser> EventOrganisers { get; set; }
        = new List<EventOrganiser>();

    public ICollection<Category> Categories { get; set; }
        = new List<Category>();
}
public class EventOrganiser
{
    public int EventOrganiserID { get; set; }

    public int EventID { get; set; }
    public Event Event { get; set; } = null!;

    public int OrganizerID { get; set; }
    public Organiser Organiser { get; set; } = null!;
    public object Name { get; internal set; }
    public object OrganizationName { get; internal set; }
    public object ContactInfo { get; internal set; }
    public string PasswordHash { get; internal set; }

    public class Category
    {
        public int CategoryID { get; set; }

        public int EventID { get; set; }
        public Event Event { get; set; } = null!;

        [Required, MaxLength(100)]
        public string Name { get; set; } = "";

        public decimal Distance { get; set; }

        public int? AgeRestriction { get; set; }
    }

    public class Registration
    {
        public int RegistrationID { get; set; }

        public int ParticipantID { get; set; }
        public Participant Participant { get; set; } = null!;

        public int CategoryID { get; set; }
        public Category Category { get; set; } = null!;

        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(30)]
        public string PaymentStatus { get; set; } = "Pending";

        public Result? Result { get; set; }
        public object Event { get; internal set; }
    }

    public class Result
    {
        public int ResultID { get; set; }

        public int RegistrationID { get; set; }
        public Registration Registration { get; set; } = null!;

        public TimeSpan? FinishTime { get; set; }

        public int? Position { get; set; }

        [MaxLength(500)]
        public string? SplitTimes { get; set; }
    }
}

