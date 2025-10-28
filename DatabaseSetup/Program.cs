using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SchoolAiChatbotBackend.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using SchoolAiChatbotBackend.Services;

namespace DatabaseSetupConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("School AI Chatbot Database Setup Console");
            Console.WriteLine("===============================================");
            
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: dotnet run <mysql_password>");
                Console.WriteLine("Example: dotnet run \"your_password_here\"");
                return;
            }

            string password = args[0];
            string connectionString = $"Server=school-ai-mysql-server.mysql.database.azure.com;Database=flexibleserverdb;Uid=adminuser;Pwd={password};SslMode=Required;";

            var services = new ServiceCollection();

            
            // Add configuration
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"ConnectionStrings:DefaultConnection", connectionString},
                    {"DatabaseProvider", "MySql"}
                })
                .Build();
            
            services.AddSingleton<IConfiguration>(configuration);
            
            // Add DbContext
              services.AddDbContext<AppDbContext>(options =>
                options.UseMySql(
                    connectionString,
                    new MySqlServerVersion(new Version(8, 0, 36))
                ));

            var serviceProvider = services.BuildServiceProvider();



// inside WebApplication.CreateBuilder(args) setup

builder.Configuration
       .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
       .AddEnvironmentVariables(); // make sure environment vars are loaded

// Register OpenAiChatService using a factory that reads config
builder.Services.AddScoped<OpenAiChatService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();

    // Try both forms: config key with colon and env var with double-underscore
    var apiKey = config["OpenAI:ApiKey"] ?? config["OpenAI__ApiKey"] ?? config["OpenAI_ApiKey"];

    if (string.IsNullOrWhiteSpace(apiKey))
        throw new InvalidOperationException("OpenAI ApiKey not found. Set OpenAI__ApiKey in App Settings or OpenAI:ApiKey in config.");

    return new OpenAiChatService(apiKey);
});

// If controllers depend on an interface, map it:
builder.Services.AddScoped<IOpenAiChatService>(sp => sp.GetRequiredService<OpenAiChatService>());


            try
            {
                Console.WriteLine("Testing database connection...");
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                // Test connection
                await context.Database.CanConnectAsync();
                Console.WriteLine("✓ Database connection successful!");

                Console.WriteLine("Creating database tables...");
                await context.Database.EnsureCreatedAsync();
                Console.WriteLine("✓ Database tables created!");

                Console.WriteLine("Seeding database with test data...");
                await DatabaseSeeder.SeedAsync(scope.ServiceProvider);
                Console.WriteLine("✓ Database seeded successfully!");

                // Verify data
                var schoolCount = await context.Schools.CountAsync();
                var userCount = await context.Users.CountAsync();
                var faqCount = await context.Faqs.CountAsync();
                
                Console.WriteLine($"\nDatabase verification:");
                Console.WriteLine($"Schools: {schoolCount}");
                Console.WriteLine($"Users: {userCount}");
                Console.WriteLine($"FAQs: {faqCount}");
                
                Console.WriteLine("\n✅ Database setup completed successfully!");
                Console.WriteLine("\nNext steps:");
                Console.WriteLine("1. Add MYSQL_PASSWORD to GitHub Secrets");
                Console.WriteLine("2. Push your changes to deploy with MySQL");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine($"Details: {ex.InnerException?.Message}");
                Environment.Exit(1);
            }
        }
    }
}