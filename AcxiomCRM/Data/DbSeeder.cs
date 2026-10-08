using Microsoft.AspNetCore.Identity;
using AcxiomCRM.Models;

namespace AcxiomCRM.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        string[] roles = { Roles.Admin, Roles.Manager, Roles.Sales };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        string adminEmail = "admin@acxiomcrm.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.Now
            };

            var createResult = await userManager.CreateAsync(adminUser, "Admin@123");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, Roles.Admin);
            }
        }

        string managerEmail = "manager@acxiomcrm.com";
        var managerUser = await userManager.FindByEmailAsync(managerEmail);
        if (managerUser == null)
        {
            managerUser = new ApplicationUser
            {
                UserName = managerEmail,
                Email = managerEmail,
                FullName = "CRM Manager",
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.Now
            };

            var createResult = await userManager.CreateAsync(managerUser, "Manager@123");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(managerUser, Roles.Manager);
            }
        }

        string salesEmail = "sales@acxiomcrm.com";
        var salesUser = await userManager.FindByEmailAsync(salesEmail);
        if (salesUser == null)
        {
            salesUser = new ApplicationUser
            {
                UserName = salesEmail,
                Email = salesEmail,
                FullName = "Sales Representative",
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.Now
            };

            var createResult = await userManager.CreateAsync(salesUser, "Sales@123");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(salesUser, Roles.Sales);
            }
        }
    }
}
