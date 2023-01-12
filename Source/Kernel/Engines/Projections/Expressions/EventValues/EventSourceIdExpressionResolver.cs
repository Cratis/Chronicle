// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;
using Aksio.Cratis.Shared.Events;

namespace Aksio.Cratis.Kernel.Engines.Projections.Expressions.EventValues;

/// <summary>
/// Represents a <see cref="IModelPropertyExpressionResolver"/> for resolving value from <see cref="EventSourceId"/> of the <see cref="AppendedEvent"/>.
/// </summary>
public class EventSourceIdExpressionResolver : IEventValueProviderExpressionResolver
{
    /// <inheritdoc/>
    public bool CanResolve(string expression) => expression == "$eventSourceId";

    /// <inheritdoc/>
    public ValueProvider<AppendedEvent> Resolve(string expression) => EventValueProviders.EventSourceId;
}
