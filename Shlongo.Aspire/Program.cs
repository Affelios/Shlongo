var builder = DistributedApplication.CreateBuilder(args);

var mongodb = builder.AddMongoDB("shlongo-mongo")
    .WithMongoExpress();

builder.AddProject<Projects.Shlongo_Examples_Api>("shlongo-examples-api")
    .WithReference(mongodb)
    .WaitFor(mongodb);

builder.Build().Run();
