// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues;

/// <summary>
/// Represents a <see cref="IEventValueProviderExpressionResolver"/> for resolving a clear of a scalar read model member.
/// </summary>
/// <remarks>
/// The expression carries no operand: <see cref="WellKnownExpressions.Null"/> always resolves to no value at all,
/// whatever the event holds. It is deliberately distinct from <c>$value(...)</c>, whose operand is captured as text -
/// routing a clear through that would write the literal characters of the operand instead of clearing anything.
/// </remarks>
public class NullExpressionResolver : IEventValueProviderExpressionResolver
{
    /// <inheritdoc/>
    public bool CanResolve(string expression) => expression == WellKnownExpressions.Null;

    /// <inheritdoc/>
    public ValueProvider<AppendedEvent> Resolve(string expression) => EventValueProviders.Null;
}
