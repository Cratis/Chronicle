// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.Expressions;

/// <summary>
/// Represents an implementation of <see cref="IEventValueExpression"/> for representing the event source id.
/// </summary>
public class EventSourceIdExpression : IEventValueExpression
{
    /// <inheritdoc/>
    public string Build() => "$eventSourceId";
}
