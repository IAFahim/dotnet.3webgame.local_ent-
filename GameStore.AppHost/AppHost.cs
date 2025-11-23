var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL with data volume for persistence
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

// Add database with the exact connection string name from appsettings
var database = postgres.AddDatabase("DefaultConnection");

// Add the Rest API project with database reference
builder.AddProject<Projects.Rest>("rest")
    .WithReference(database)
    .WaitFor(database);

builder.Build().Run();
