using RaceDay.Data.API.Data;
using Microsoft.AspNetCore.Authentication.Cookies;


var builder = WebApplication.CreateBuilder(args);

// This allows the API to use controllers for handling requests.
builder.Services.AddControllers();

// These services enable Swagger so I can test my API in the browser.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// This connects my application to the SQL Server database.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// I added memory cache because session management needs somewhere
// to keep the session information while the user is logged in.
builder.Services.AddDistributedMemoryCache();

// This sets up sessions and makes them expire after 30 minutes.
builder.Services.AddSession(options =>
{
options.Cookie.Name = ".EventManagement.Session";
options.Cookie.HttpOnly = true;
options.Cookie.IsEssential = true;
options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
options.IdleTimeout = TimeSpan.FromMinutes(30);
});

// Cookie authentication keeps track of the logged-in user's identity.
builder.Services.AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
{
options.Cookie.Name = ".EventManagement.Auth";
options.Cookie.HttpOnly = true;
options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
options.Cookie.SameSite = SameSiteMode.Strict;
options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
options.SlidingExpiration = true;

// The API returns 401 when the user is not logged in.
options.Events.OnRedirectToLogin = context =>
{
context.Response.StatusCode =
    StatusCodes.Status401Unauthorized;
return Task.CompletedTask;
};

// The API returns 403 when the user does not have permission.
options.Events.OnRedirectToAccessDenied = context =>
{
context.Response.StatusCode =
    StatusCodes.Status403Forbidden;
return Task.CompletedTask;
};
});

// This enables role-based access control in my API.
builder.Services.AddAuthorization();

var app = builder.Build();

// These lines enable Swagger and the Swagger testing page.
app.UseSwagger();
app.UseSwaggerUI();

// This makes sure requests are redirected to HTTPS.
app.UseHttpsRedirection();

// The middleware must run in this order for authentication to work.
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// This connects incoming requests to my controller endpoints.
app.MapControllers();

app.Run();

// I added this so my test project can access the Program class.
public partial class Program { }