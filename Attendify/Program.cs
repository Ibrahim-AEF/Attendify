
using Attendify.Helpers;
using Domain.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Presistance;
using Presistance.Data;
using System.Text;

namespace Attendify
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            //Add DbContext
            builder.Services.AddDbContext<AppDbContext>(options =>
              options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Add Identity
            builder.Services.AddIdentity<Admin, IdentityRole>()
              .AddEntityFrameworkStores<AppDbContext>()
              .AddDefaultTokenProviders();

            // Configure Identity options
            builder.Services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 1;

                options.User.RequireUniqueEmail = true;
            });

            var jwtSettings = builder.Configuration.GetSection("Jwt");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]);

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            // Add JwtHelper
            builder.Services.AddScoped<JwtHelper>();

            // Add AutoMapper
            builder.Services.AddAutoMapper(typeof(Program));

            builder.Services.AddSingleton<IPasswordHasher<Instructor>, PasswordHasher<Instructor>>();
            builder.Services.AddSingleton<IPasswordHasher<Student>, PasswordHasher<Student>>();

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            // Add this before app.Run()
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<AppDbContext>();
                    var userManager = services.GetRequiredService<UserManager<Admin>>();
                    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

                    // Wait for initialization to complete
                    await DbInitializer.Initialize(context, userManager, roleManager);

                    // Verify admin exists
                    var admin = await userManager.FindByEmailAsync("Ahmed123@must.edu.eg");
                    Console.WriteLine(admin != null
                        ? $"Admin found with ID: {admin.Id}"
                        : "ADMIN NOT CREATED!");
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Database initialization failed");
                    throw;  // Stop app if initialization fails
                }
            }

            app.Run();
        }
    }
}
