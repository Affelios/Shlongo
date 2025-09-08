var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Shlongo_Examples_Api>("shlongo-examples-api");

builder.Build().Run();
