using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SafeRouteSA.Web.Data;

var builder = WebApplication.CreateBuilder(args);

// DATABASE CONNECTION
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "DefaultConnection was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// IDENTITY CONFIGURATION
builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;

        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// CREATE DEFAULT ADMIN ROLE AND ACCOUNT
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    var userManager = scope.ServiceProvider
        .GetRequiredService<UserManager<IdentityUser>>();

    const string adminRole = "Admin";

    // Create Admin role if it does not exist
    if (!await roleManager.RoleExistsAsync(adminRole))
    {
        var roleResult = await roleManager.CreateAsync(
            new IdentityRole(adminRole));

        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join("; ",
                    roleResult.Errors.Select(e => e.Description)));
        }
    }

    // Admin credentials come from User Secrets
    var adminEmail = app.Configuration["AdminSeed:Email"];
    var adminPassword = app.Configuration["AdminSeed:Password"];

    if (!string.IsNullOrWhiteSpace(adminEmail) &&
        !string.IsNullOrWhiteSpace(adminPassword))
    {
        var adminUser = await userManager.FindByEmailAsync(
            adminEmail);

        if (adminUser == null)
        {
            adminUser = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(
                adminUser,
                adminPassword);

            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Admin creation failed: " +
                    string.Join("; ",
                        createResult.Errors.Select(e => e.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(
            adminUser, adminRole))
        {
            var roleResult = await userManager.AddToRoleAsync(
                adminUser, adminRole);

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Admin role assignment failed: " +
                    string.Join("; ",
                        roleResult.Errors.Select(e => e.Description)));
            }
        }
    }
}

// HTTP PIPELINE
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages();

app.Run();
