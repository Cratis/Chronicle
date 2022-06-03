// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Projections.Outbox;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Types;
using Orleans;

namespace Aksio.Cratis.Events.Outbox;

/// <summary>
/// Represents an implementation of <see cref="IOutboxProjectionsRegistrar"/>.
/// </summary>
public class OutboxProjectionsRegistrar : IOutboxProjectionsRegistrar
{
    readonly IClusterClient _clusterClient;
    readonly IExecutionContextManager _executionContextManager;
    readonly IInstancesOf<IOutboxProjections> _outboxProjections;
    readonly IEnumerable<OutboxProjectionsDefinition> _outboxProjectionsDefinition;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxProjectionsRegistrar"/> class.
    /// </summary>
    /// <param name="clusterClient">Orleans <see cref="IClusterClient"/>.</param>
    /// <param name="eventTypes">Registered <see cref="IEventTypes"/>.</param>
    /// <param name="jsonSchemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating schemas for projections.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for establishing execution context.</param>
    /// <param name="outboxProjections">All instances of <see cref="IOutboxProjections"/>.</param>
    public OutboxProjectionsRegistrar(
        IClusterClient clusterClient,
        IEventTypes eventTypes,
        IJsonSchemaGenerator jsonSchemaGenerator,
        IExecutionContextManager executionContextManager,
        IInstancesOf<IOutboxProjections> outboxProjections)
    {
        _clusterClient = clusterClient;
        _executionContextManager = executionContextManager;
        _outboxProjections = outboxProjections;
        _outboxProjectionsDefinition = _outboxProjections.Select(projections =>
        {
            var builder = new OutboxProjectionsBuilder(eventTypes, jsonSchemaGenerator, projections.Identifier);
            projections.Define(builder);
            return builder.Build();
        }).ToArray();
    }

    /// <inheritdoc/>
    public Task DiscoverAndRegisterAll()
    {
        _executionContextManager.Establish(ExecutionContextManager.GlobalMicroserviceId);

        Console.WriteLine(_outboxProjectionsDefinition);
        Console.WriteLine(_clusterClient);

        return Task.CompletedTask;
    }
}
