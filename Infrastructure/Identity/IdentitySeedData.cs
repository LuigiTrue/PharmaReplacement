using Microsoft.AspNetCore.Identity;
using RepyPharma.Domain.Entities;

namespace RepyPharma.Infrastructure.Identity;

public static class IdentitySeedData
{
    public const string DefaultAdminUserName = "admin";
    public const string DefaultAdminEmail = "admin@repypharma.local";
    public const string DefaultAdminPassword = "Admin@123456";

    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in AuthRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var adminUserName = configuration["IdentitySeed:AdminUserName"] ?? DefaultAdminUserName;
        var adminEmail = configuration["IdentitySeed:AdminEmail"] ?? DefaultAdminEmail;
        var adminPassword = configuration["IdentitySeed:AdminPassword"] ?? DefaultAdminPassword;

        var admin = await userManager.FindByNameAsync(adminUserName)
            ?? await userManager.FindByEmailAsync(adminEmail);

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminUserName,
                Email = adminEmail,
                EmailConfirmed = true,
                Name = "Administrador",
                IsActive = true
            };

            var result = await userManager.CreateAsync(admin, adminPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException(GetIdentityErrorMessage("Falha ao criar usuario administrador inicial", result));
        }

        if (!admin.IsActive)
        {
            admin.IsActive = true;
            await userManager.UpdateAsync(admin);
        }

        if (!await userManager.IsInRoleAsync(admin, AuthRoles.Admin))
            await userManager.AddToRoleAsync(admin, AuthRoles.Admin);
    }

    private static string GetIdentityErrorMessage(string message, IdentityResult result)
    {
        return $"{message}: {string.Join("; ", result.Errors.Select(error => error.Description))}";
    }
}
