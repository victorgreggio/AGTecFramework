using AGTec.Reference.Infrastructure;
using AGTec.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddAGTecServiceDefaults();

// Same Marten + Wolverine core as the API; this host *listens* for integration events and
// projects the read model. The transport is selected by configuration, identical to the API.
builder.AddReferencePlatform(MessagingRole.Listener);

builder.Build().Run();
