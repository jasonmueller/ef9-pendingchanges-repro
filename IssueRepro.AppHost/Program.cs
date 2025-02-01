var builder = DistributedApplication.CreateBuilder(args);

var db = builder.AddPostgres("postgres").WithDataVolume("issuedata").AddDatabase("issues");

builder.AddProject<Projects.IssueRepro_Web>("web").WithReference(db).WaitFor(db);

builder.Build().Run();
