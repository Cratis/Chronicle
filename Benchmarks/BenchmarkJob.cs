// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json.Nodes;
using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Schemas;
using Aksio.Cratis.Schemas;
using Aksio.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Benchmarks;

public abstract class BenchmarkJob
{
    protected IGrainFactory GrainFactory { get; private set; } = null!;
    protected IExecutionContextManager? ExecutionContextManager { get; private set; }
    protected IEventSerializer? EventSerializer { get; private set; }
    protected ISchemaStore? SchemaStore { get; private set; }
    protected IJsonSchemaGenerator? SchemaGenerator { get; private set; }
    protected virtual IEnumerable<Type> EventTypes => Enumerable.Empty<Type>();

    [GlobalSetup]
    public void GlobalSetup()
    {
        GrainFactory = GlobalVariables.ServiceProvider.GetRequiredService<IGrainFactory>();
        ExecutionContextManager = GlobalVariables.ServiceProvider.GetRequiredService<IExecutionContextManager>();
        EventSerializer = GlobalVariables.ServiceProvider.GetRequiredService<IEventSerializer>();
        SchemaStore = GlobalVariables.ServiceProvider.GetRequiredService<ISchemaStore>();
        SchemaGenerator = GlobalVariables.ServiceProvider.GetRequiredService<IJsonSchemaGenerator>();

        foreach (var eventType in EventTypes)
        {
            var eventTypeAttribute = eventType.GetCustomAttribute<EventTypeAttribute>()!;
            SchemaStore.Register(eventTypeAttribute.Type, eventType.Name, SchemaGenerator.Generate(eventType));
        }

        Setup();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
    }

    protected virtual void Setup()
    {
    }

    protected JsonObject SerializeEvent(object @event) => EventSerializer!.Serialize(@event).GetAwaiter().GetResult();
}
