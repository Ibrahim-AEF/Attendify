using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Presistance.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Presistance
{
    public static class DbInitializer
    {
        public static async Task Initialize(AppDbContext context,
                                  UserManager<Admin> userManager,
                                  RoleManager<IdentityRole> roleManager)
        {
            // Use migrations instead of EnsureCreated
            await context.Database.MigrateAsync();

            // Create roles
            string[] roleNames = { "Admin", "Instructor" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Create admin user
            var adminEmail = "Ahmed123@must.edu.eg";
            var admin = await userManager.FindByEmailAsync(adminEmail);

            if (admin == null)
            {
                admin = new Admin
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Ahmed Mohamed",
                    PhoneNumber = "01151558580",
                    EmailConfirmed = true  // Important for login
                };

                var result = await userManager.CreateAsync(admin, "Admin@123");

                if (!result.Succeeded)
                {
                    throw new Exception($"Admin creation failed: {string.Join(", ", result.Errors)}");
                }

                await userManager.AddToRoleAsync(admin, "Admin");

                // Verify creation
                var createdAdmin = await userManager.FindByEmailAsync(adminEmail);
                if (createdAdmin == null)
                {
                    throw new Exception("Admin was not found immediately after creation!");
                }
            }
        }
    }
}
