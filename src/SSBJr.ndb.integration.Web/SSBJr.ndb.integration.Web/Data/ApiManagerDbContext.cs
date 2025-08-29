using Microsoft.EntityFrameworkCore;
using SSBJr.ndb.integration.Web.Models;
using SSBJr.ndb.integration.Web.Services;
using System.Text.Json;

namespace SSBJr.ndb.integration.Web.Data;

public class ApiManagerDbContext : DbContext
{
    public ApiManagerDbContext(DbContextOptions<ApiManagerDbContext> options) : base(options)
    {
    }

    public DbSet<ApiInterface> ApiInterfaces { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<ApiKey> ApiKeys { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure ApiInterface
        modelBuilder.Entity<ApiInterface>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Version).IsRequired().HasMaxLength(50);
            entity.Property(e => e.GraphQLSchema).HasColumnType("text");
            entity.Property(e => e.SwaggerJson).HasColumnType("text");
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);

            // Convert complex objects to JSON
            entity.Property(e => e.Infrastructure)
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                      v => JsonSerializer.Deserialize<InfrastructureConfig>(v, (JsonSerializerOptions?)null) ?? new InfrastructureConfig());

            entity.Property(e => e.Security)
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                      v => JsonSerializer.Deserialize<SecurityConfig>(v, (JsonSerializerOptions?)null) ?? new SecurityConfig());

            entity.Property(e => e.Monitoring)
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                      v => JsonSerializer.Deserialize<MonitoringConfig>(v, (JsonSerializerOptions?)null) ?? new MonitoringConfig());

            entity.Property(e => e.Tags)
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                      v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

            entity.Property(e => e.Metadata)
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                      v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>());

            entity.Property(e => e.DeploymentInfo)
                  .HasConversion(
                      v => v != null ? JsonSerializer.Serialize(v, (JsonSerializerOptions?)null) : null,
                      v => v != null ? JsonSerializer.Deserialize<DeploymentInfo>(v, (JsonSerializerOptions?)null) : null);

            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Configure AuditLog
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityId).HasMaxLength(50);
            entity.Property(e => e.EntityType).HasMaxLength(50);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.AdditionalData).HasColumnType("text");
            entity.Property(e => e.Source).IsRequired().HasMaxLength(50);

            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.EntityId);
            entity.HasIndex(e => e.Action);
        });

        // Configure User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);

            entity.Property(e => e.Permissions)
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                      v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Configure ApiKey
        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(500);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);

            entity.Property(e => e.Permissions)
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                      v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

            entity.HasIndex(e => e.Key).IsUnique();
            entity.HasIndex(e => e.UserId);

            entity.HasOne<User>()
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed admin user
        var adminUserId = Guid.NewGuid();
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = adminUserId,
            Username = "admin",
            Email = "admin@ssbjr.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), // In production, use proper hashing
            FullName = "System Administrator",
            Role = "Administrator",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Permissions = new List<string> { "api.create", "api.read", "api.update", "api.delete", "api.deploy", "audit.read" }
        });

        // Seed demo API interface
        var demoApiId = Guid.NewGuid();
        modelBuilder.Entity<ApiInterface>().HasData(new ApiInterface
        {
            Id = demoApiId,
            Name = "Demo GraphQL API",
            Description = "API de demonstração com GraphQL e infraestrutura completa",
            Type = ApiType.GraphQL,
            Version = "1.0.0",
            GraphQLSchema = @"
                type Query {
                    users: [User]
                    user(id: ID!): User
                }
                
                type User {
                    id: ID!
                    name: String!
                    email: String!
                    posts: [Post]
                }
                
                type Post {
                    id: ID!
                    title: String!
                    content: String!
                    author: User!
                }
            ",
            Status = ApiStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system",
            Tags = new List<string> { "demo", "graphql", "sample" },
            Infrastructure = new InfrastructureConfig
            {
                Database = new DatabaseConfig
                {
                    Type = DatabaseType.PostgreSQL,
                    ConnectionString = "postgres://user:password@localhost:5432/demo_db",
                    EnableReadReplicas = true,
                    MaxConnections = 100
                },
                Messaging = new MessagingConfig
                {
                    Type = MessagingType.RabbitMQ,
                    ConnectionString = "amqp://guest:guest@localhost:5672/",
                    Queues = new List<string> { "notifications", "events" }
                },
                Cache = new CacheConfig
                {
                    Type = CacheType.Redis,
                    ConnectionString = "redis://localhost:6379",
                    DefaultTTL = 3600
                }
            },
            Security = new SecurityConfig
            {
                Authentication = new AuthenticationConfig
                {
                    Type = AuthenticationType.JWT
                },
                RateLimit = new RateLimitConfig
                {
                    Enabled = true,
                    RequestsPerMinute = 100
                }
            },
            Monitoring = new MonitoringConfig
            {
                Logging = new LoggingConfig
                {
                    Level = SSBJr.ndb.integration.Web.Models.LogLevel.Information,
                    EnableStructuredLogging = true
                },
                Metrics = new MetricsConfig
                {
                    Enabled = true,
                    Provider = "Prometheus"
                },
                Audit = new AuditConfig
                {
                    Enabled = true,
                    AuditRequests = true
                }
            },
            Metadata = new Dictionary<string, object>()
        });
    }
}

// Additional entities for authentication and API management
public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public List<string> Permissions { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
}

public class ApiKey
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}