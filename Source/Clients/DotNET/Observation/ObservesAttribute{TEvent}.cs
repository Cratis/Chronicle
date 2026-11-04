// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Declares that a batch method observes events of type <typeparamref name="TEvent"/>.
/// </summary>
/// <typeparam name="TEvent">The event type observed.</typeparam>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ObservesAttribute<TEvent> : ObservesAttribute
    where TEvent : class
{
    /// <inheritdoc/>
    public override Type EventType => typeof(TEvent);
}
