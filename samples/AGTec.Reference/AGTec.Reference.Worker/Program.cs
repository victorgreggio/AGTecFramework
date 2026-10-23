using AGTec.Reference.Infrastructure;
using AGTec.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddAGTecServiceDefaults();

// Same EF Core/PostgreSQL + Wolverine core as the API; Marten holds its document projection.
// projects the read model. The transport is selected by configuration, identical to the API.
builder.AddReferencePlatform(MessagingRole.Listener);

builder.Build().Run();
