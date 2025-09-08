var builder = DistributedApplication.CreateBuilder(args);

var mongo = builder.AddMongoDB("shlongo-mongo")
    .WithMongoExpress();

builder.AddProject<Projects.Shlongo_Examples_Api>("shlongo-examples-api")
    .WaitFor(mongo);

builder.Build().Run();
