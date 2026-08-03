var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage").RunAsEmulator(azurite => azurite.WithArgs("--silent"));

builder.AddAzureFunctionsProject<Projects.Blog_Portfolio_Host>("host")
    .WithHostStorage(storage)
    .WithExternalHttpEndpoints();

await builder.Build().RunAsync();
