// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Expressions.EventValues;

/// <summary>
/// Represents a <see cref="IReadModelPropertyExpressionResolver"/> for resolving value from a property on the content of an <see cref="AppendedEvent"/>.
/// </summary>
public class EventContentExpressionResolver : IEventValueProviderExpressionResolver
{
    /// <inheritdoc/>
    public bool CanResolve(string expression) => !expression.StartsWith('$');

    /// <inheritdoc/>
    public ValueProvider<AppendedEvent> Resolve(string expression) => EventValueProviders.EventContent(expression);
}
