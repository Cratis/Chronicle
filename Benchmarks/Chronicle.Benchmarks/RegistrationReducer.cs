// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// A distinct reducer per closed marker pair, owning its own closed read model type.
/// </summary>
/// <typeparam name="TLeft">The first identity marker.</typeparam>
/// <typeparam name="TRight">The second identity marker.</typeparam>
public class RegistrationReducer<TLeft, TRight> : IReducerFor<RegistrationReadModel<TLeft, TRight>>
{
    /// <summary>
    /// Reduces the shared event type; the registration benchmark does not append events.
    /// </summary>
    /// <param name="event">The event to reduce.</param>
    /// <param name="current">The current model, if present.</param>
    /// <returns>The updated read model.</returns>
    public RegistrationReadModel<TLeft, TRight> Reduce(BenchmarkEvent @event, RegistrationReadModel<TLeft, TRight>? current)
    {
        _ = @event;
        return new() { Count = (current?.Count ?? 0) + 1 };
    }
}
