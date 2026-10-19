# Destination Backend: ASP.NET Core (.NET) + SQL Server Blueprint

This document details the architectural blueprint and directory structure for converting the Project Management System (PMS) backend from Python FastAPI to **ASP.NET Core (.NET 8/9)**. It integrates with **Entity Framework Core (EF Core)** and **Microsoft SQL Server (MSSQL)**.

---

## 1. Directory Structure: Clean Architecture (Onion Architecture)

For an enterprise application like PMS, we use **Clean Architecture** to separate business logic from external frameworks, database ORMs, and UI/API layers.

```
pmsbackend/
├── TechSpearPMS.sln                     # .NET Solution File
│
├── TechSpearPMS.Domain/                 # Core Domain Layer (Pure C#, no dependencies)
│   ├── Common/                          # Base entities and core abstractions
│   │   ├── BaseEntity.cs                # Id definition (int or Guid)
│   │   └── AuditableEntity.cs           # Audit fields (C# equivalent of AuditMixin)
│   ├── Entities/                        # Database entities (models)
│   │   ├── User.cs
│   │   ├── Project.cs
│   │   ├── Task.cs
│   │   └── ...
│   ├── Enums/                           # App-wide enumerations (PriorityLevel, TaskStatus)
│   └── Exceptions/                      # Custom domain exceptions
│
├── TechSpearPMS.Application/            # Application Logic Layer (DTOs, Services, Interfaces)
│   ├── Common/                          # Interfaces & application configurations
│   │   ├── Interfaces/                  # IApplicationDbContext, IBlobStorageService
│   │   └── Models/                      # PagedList, Result templates
│   ├── DTOs/                            # Data Transfer Objects (Request/Response shapes)
│   │   ├── UserDto.cs
│   │   ├── ProjectDto.cs
│   │   └── ...
│   ├── Mapping/                         # Mapping configurations (AutoMapper or Mapster)
│   ├── Services/                        # Business logic services
│   │   ├── Interfaces/                  # IProjectService, ITaskService, IUserService
│   │   └── Implementations/             # ProjectService.cs, TaskService.cs
│   └── Validators/                      # FluentValidation schemas for requests
│
├── TechSpearPMS.Infrastructure/         # Infrastructure Layer (EF Core, Azure Services)
│   ├── Data/                            # DbContext, configuration schemas, seeder
│   │   ├── ApplicationDbContext.cs      # EF Core DbContext mapping all tables & relations
│   │   ├── ApplicationDbContextSeed.cs  # Seeds lookup values (Roles, Priorities)
│   │   └── Interceptors/                # Automates updated_at and created_at timestamps
│   ├── Migrations/                      # SQL Server database migration scripts
│   ├── Services/                        # Azure Blob Storage, Graph API integrations
│   └── DependencyInjection.cs           # Registers database contexts and services
│
└── TechSpearPMS.API/                    # Presentation / Web API Layer (Controllers & Config)
    ├── Controllers/                     # REST API Controllers (grouped v1)
    │   ├── BaseApiController.cs         # Shared router routes & metadata
    │   ├── AuthController.cs            # Microsoft token verification
    │   ├── ProjectsController.cs
    │   └── TasksController.cs
    ├── Middleware/                      # Global exception handles, request loggers
    ├── appsettings.json                 # DB and Azure configuration parameters
    ├── appsettings.Development.json     # Developer-machine configuration overrides
    └── Program.cs                       # App bootstrapper, CORS, authentication registration
```

---

## 2. Core Implementation Blueprints (C# / EF Core)

### 2.1 The Auditing Base (`AuditableEntity.cs`)
In `Domain/Common/AuditableEntity.cs`, represent Python's `AuditMixin`:

```csharp
namespace TechSpearPMS.Domain.Common;

public abstract class BaseEntity
{
    public int Id { get; set; }
}

public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
}
```

### 2.2 Database Context & Cascade Cycles (`ApplicationDbContext.cs`)
SQL Server forbids circular references or multiple cascade paths (e.g. `users.manager_email → users.email` or cyclic cascades in tasks/projects referencing user tables). In EF Core, configure these relationships to use `DeleteBehavior.NoAction`.

In `Infrastructure/Data/ApplicationDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using TechSpearPMS.Domain.Entities;

namespace TechSpearPMS.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Task> Tasks => Set<Task>();
    // Define DbSets for other models...

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply global filter for Soft Delete
        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
        modelBuilder.Entity<Project>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Task>().HasQueryFilter(t => !t.IsDeleted);

        // Map relationships and eliminate cascade delete cycles
        modelBuilder.Entity<User>()
            .HasOne(u => u.Manager)
            .WithMany()
            .HasForeignKey(u => u.ManagerEmail)
            .HasPrincipalKey(u => u.Email)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Task>()
            .HasOne(t => t.AssignedUser)
            .WithMany()
            .HasForeignKey(t => t.AssignedTo)
            .OnDelete(DeleteBehavior.NoAction);

        // Many-to-Many configurations (e.g. UserTeamLink, UserSkillLink)
        modelBuilder.Entity<User>()
            .HasMany(u => u.Teams)
            .WithMany(t => t.Members)
            .UsingEntity(j => j.ToTable("UserTeamLink"));
    }
}
```

### 2.3 Bootstrap Configuration (`Program.cs`)
Instruct ASP.NET Core to handle CORS, database setup, JWT token authentication, and **snake_case serialization** so the React frontend does not break.

In `TechSpearPMS.API/Program.cs`:

```csharp
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using TechSpearPMS.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Add SQL Server Connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, b => b.MigrationsAssembly("TechSpearPMS.Infrastructure")));

// 2. Add Microsoft AD JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

// 3. Add Controllers and force JSON responses to utilize snake_case 
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
    });

// 4. Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("CorsOrigins").Get<string[]>() ?? new[] { "*" })
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Enable Middlewares
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

---

## 3. Command Reference & Migration Execution

To install global EF Core tools and run migrations:

```bash
# Install EF Core tool CLI globally
dotnet tool install --global dotnet-ef

# 1. Create a migration baseline from models
dotnet ef migrations add InitialMSSQLSchema -p TechSpearPMS.Infrastructure -s TechSpearPMS.API

# 2. Apply migrations to the target SQL Server DB
dotnet ef database update -p TechSpearPMS.Infrastructure -s TechSpearPMS.API
```

---

## 4. Summary of Conversion Tasks

1. Create models in C# matching the structure in [PMS-backend PROJECT_OVERVIEW.md](file:///c:/Users/Techn/source/repos/deepakraj-18/pms/PMS-backend/PROJECT_OVERVIEW.md).
2. Configure relations in DbContext Fluent API with `DeleteBehavior.NoAction`.
3. Set up the seeder to feed lookup categories on startup (Project status, roles, skills, task priorities).
4. Configure Azure Storage SDK client for .NET file handling services.
5. Create controllers mimicking FastAPI's routes and endpoints using standard HTTP decorators (`[HttpGet]`, `[HttpPost]`, etc.).
