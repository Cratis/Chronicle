using Aksio.Cratis;
using Aksio.Cratis.EventSequences;
using Basic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.UseCratis();
var app = builder.Build();
app.UseCratis();

app.MapGet("/", () =>
{
    var eventLog = app.Services.GetRequiredService<IEventLog>();
    eventLog.Append("49b9727f-64da-4d5d-bb52-8a3fc77f6a81", new MyEvent());
});


// var client = ClientBuilder
//     .SingleTenanted()
//     .Build(builder.Services);


// await client
//     .EventStore
//     .Sequences
//     .EventLog
//     .Append("49b9727f-64da-4d5d-bb52-8a3fc77f6a81", new MyEvent());

// var client = ClientBuilder
//     .MultiTenanted()
//     .Build();

// await client
//     .EventStore
//     .Sequences
//     .ForTenant(TenantId.Development)
//     .EventLog
//     .Append(EventSourceId.New(), new MyEvent());

app.Run();
