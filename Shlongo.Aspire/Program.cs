var builder = DistributedApplication.CreateBuilder(args);

var mongo = builder.AddMongoDB("shlongo-mongo")
    .WithMongoExpress();

builder.AddProject<Projects.Shlongo_Examples_Api>("shlongo-examples-api")
    .WithReference(mongo)
    .WaitFor(mongo);

builder.Build().Run();
