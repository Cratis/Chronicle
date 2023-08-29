// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Auditing;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Identities;
using Aksio.Cratis.Kernel.Schemas;
using Aksio.Execution;
using Microsoft.Extensions.DependencyInjection;
using NJsonSchema;
using IEventSequence = Aksio.Cratis.Kernel.Grains.EventSequences.IEventSequence;

namespace Benchmarks.EventSequences;

public class AppendingEvents
{
    public static readonly IEnumerable<Causation> BenchmarkCausation = new[] { new Causation(DateTimeOffset.UtcNow, "Benchmark", new Dictionary<string, string>()) };

    IEventSequence? _eventSequence;
    IExecutionContextManager? _executionContextManager;

    [Benchmark]
    public async Task EventsInSequence()
    {
        _executionContextManager?.Establish(TenantId.Development, CorrelationId.New(), GlobalVariables.MicroserviceId);
        await (_eventSequence?.Append(
           Guid.NewGuid().ToString(),
           new EventType("fd5bdc58-a224-4e74-897c-fb28aed51c8b", EventGeneration.First),
           new JsonObject(),
           BenchmarkCausation,
           Identity.System) ?? Task.CompletedTask);
    }

    [GlobalSetup]
    public void GlobalSetup()
    {
        var grainFactory = GlobalVariables.ServiceProvider.GetRequiredService<IGrainFactory>();
        _eventSequence = grainFactory.GetGrain<IEventSequence>(EventSequenceId.Log, keyExtension: new MicroserviceAndTenant(GlobalVariables.MicroserviceId, TenantId.Development));

        _executionContextManager = GlobalVariables.ServiceProvider.GetRequiredService<IExecutionContextManager>();

        _executionContextManager?.Establish(TenantId.Development, CorrelationId.New(), GlobalVariables.MicroserviceId);

        var schemaStore = GlobalVariables.ServiceProvider.GetRequiredService<ISchemaStore>();
        schemaStore.Register(new EventType("fd5bdc58-a224-4e74-897c-fb28aed51c8b", EventGeneration.First), "Test", new JsonSchema());
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
    }
}
