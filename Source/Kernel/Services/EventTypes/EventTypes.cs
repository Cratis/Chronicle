// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Contracts.Events;
using Aksio.Cratis.Kernel.Schemas;
using Aksio.DependencyInversion;
using NJsonSchema;

namespace Aksio.Cratis.Kernel.Services.Events;

/// <summary>
/// Represents an implementation of <see cref="IEventTypes"/>.
/// </summary>
public class EventTypes : IEventTypes
{
    readonly IExecutionContextManager _executionContextManager;
    readonly ProviderFor<ISchemaStore> _schemaStoreProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventTypes"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/>.</param>
    /// <param name="schemaStoreProvider">Underlying <see cref="ISchemaStore"/>.</param>
    public EventTypes(
        IExecutionContextManager executionContextManager,
        ProviderFor<ISchemaStore> schemaStoreProvider)
    {
        _executionContextManager = executionContextManager;
        _schemaStoreProvider = schemaStoreProvider;
    }

    /// <inheritdoc/>
    public async Task Register(RegisterEventTypesRequest request)
    {
        _executionContextManager.Establish(request.EventStoreName);
        foreach (var eventType in request.Types)
        {
            var schema = await JsonSchema.FromJsonAsync(eventType.Schema);
            await _schemaStoreProvider().Register(eventType.Type.ToKernel(), eventType.FriendlyName, schema);
        }
    }
}
