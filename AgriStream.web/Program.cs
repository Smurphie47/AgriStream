using AgriStream.web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddScoped<UserService>();

builder.Services.AddScoped<TelemetryService>();

builder.Services.AddScoped<DroneImageService>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";

        options.Cookie.Name = "AgriStream.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;

        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

builder.Services.AddHttpClient<FarmFunctionsService>(client =>
{
    var baseUrl = builder.Configuration["Functions:BaseUrl"]
        ?? throw new InvalidOperationException(
            "Functions:BaseUrl is missing.");

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);

    var key = builder.Configuration["Functions:Key"];

    if (!string.IsNullOrWhiteSpace(key))
    {
        client.DefaultRequestHeaders.Add("x-functions-key", key);
    }
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    var seedPassword = builder.Configuration["SeedUsers:Password"];

    if (!string.IsNullOrWhiteSpace(seedPassword))
    {
        using var scope = app.Services.CreateScope();

        var users = scope.ServiceProvider
            .GetRequiredService<UserService>();

        await users.CreateUserIfMissingAsync(
            "admin", seedPassword, "Admin");

        await users.CreateUserIfMissingAsync(
            "farmer", seedPassword, "Farmer");

        await users.CreateUserIfMissingAsync(
            "agronomist", seedPassword, "Agronomist");
    }
}


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();