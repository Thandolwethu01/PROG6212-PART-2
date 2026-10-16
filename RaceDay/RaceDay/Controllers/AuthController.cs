using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data.API.Data;
using RaceDay.DTOs.API.DTOs;
using RaceDay.Models.API.Models;
using System.Data.Entity;
using System.Security.Claims;

namespace RaceDay.Controllers.API.Controllers;
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly PasswordHasher<object> _hasher = new();

    public AuthController(ApplicationDbContext db) => _db = db;

    /// <summary>Register an organiser or participant account.</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegistDto dto)
    {
        var role = dto.Role.Trim();

        if (role != "Organiser" && role != "Participant")
            return BadRequest("Role must be Organiser or Participant.");

        var email = dto.Email.Trim().ToLowerInvariant();

        if (await _db.Organisers.AnyAsync(x => x.ContactInfo == email) ||
            await _db.Participants.AnyAsync(x => x.ContactInfo == email))
            return Conflict("An account with this email already exists.");

        if (role == "Participant")
        {
            if (string.IsNullOrWhiteSpace(dto.FirstName) ||
                string.IsNullOrWhiteSpace(dto.LastName) ||
                dto.DateOfBirth == null ||
                dto.DateOfBirth.Value.Date > DateTime.UtcNow.Date)
                return BadRequest(
                    "First name, last name and a valid date of birth are required.");

            var participant = new Participant
            {
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Gender = dto.Gender.Trim(),
                DateOfBirth = dto.DateOfBirth.Value.Date,
                ContactInfo = email
            };

            participant.PasswordHash =
                _hasher.HashPassword(participant, dto.Password);

            _db.Participants.Add(participant);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(dto.Name) ||
                string.IsNullOrWhiteSpace(dto.OrganizationName))
                return BadRequest(
                    "Name and organization name are required for organisers.");

            var organiser = new EventOrganiser
            {
                Name = dto.Name.Trim(),
                OrganizationName = dto.OrganizationName.Trim(),
                ContactInfo = email
            };

            organiser.PasswordHash =
                _hasher.HashPassword(organiser, dto.Password);

            _db.Organisers.Add(organiser);
        }

        await _db.SaveChangesAsync();

        return StatusCode(201, new
        {
            message = "Account created successfully.",
            role
        });
    }

    /// <summary>Log in and receive an authentication cookie.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        var participant = await _db.Participants
            .SingleOrDefaultAsync(x => x.ContactInfo == email);

        var organiser = await _db.Organisers
            .SingleOrDefaultAsync(x => x.ContactInfo == email);

        string role;
        string userId;
        object account;
        string passwordHash;

        if (participant != null)
        {
            account = participant;
            passwordHash = participant.PasswordHash;
            role = "Participant";
            userId = participant.ParticipantID.ToString();
        }
        else if (organiser != null)
        {
            account = organiser;
            passwordHash = organiser.PasswordHash;
            role = "Organiser";
            userId = organiser.OrganizerID.ToString();
        }
        else
        {
            return Unauthorized("Invalid email or password.");
        }

        var verification = _hasher.VerifyHashedPassword(
            account, passwordHash, dto.Password);

        if (verification == PasswordVerificationResult.Failed)
            return Unauthorized("Invalid email or password.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        HttpContext.Session.SetString("UserId", userId);
        HttpContext.Session.SetString("Role", role);

        return Ok(new { message = "Login successful.", role });
    }

    /// <summary>Log out and invalidate the current authentication cookie.</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        HttpContext.Session.Clear();

        return Ok(new { message = "Logged out successfully." });
    }
}