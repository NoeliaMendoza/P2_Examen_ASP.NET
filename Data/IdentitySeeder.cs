using Microsoft.AspNetCore.Identity;

namespace NorthwindApp.Data;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

        // Crear roles
        string[] roles = { "Admin", "Employee" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Crear usuario admin
        string emailAdmin = "admin@espe.edu.ec";
        string passwordAdmin = "Admin123*";

        var admin = await userManager.FindByEmailAsync(emailAdmin);
        if (admin == null)
        {
            admin = new IdentityUser { UserName = emailAdmin, Email = emailAdmin };
            await userManager.CreateAsync(admin, passwordAdmin);
        }
        if (!await userManager.IsInRoleAsync(admin, "Admin"))
        {
            await userManager.AddToRoleAsync(admin, "Admin");
        }

        // Crear usuario empleado
        string emailEmployee = "empleado@espe.edu.ec";
        string passwordEmployee = "Empleado123*";

        var employee = await userManager.FindByEmailAsync(emailEmployee);
        if (employee == null)
        {
            employee = new IdentityUser { UserName = emailEmployee, Email = emailEmployee };
            await userManager.CreateAsync(employee, passwordEmployee);
        }
        if (!await userManager.IsInRoleAsync(employee, "Employee"))
        {
            await userManager.AddToRoleAsync(employee, "Employee");
        }
    }
}
