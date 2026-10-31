// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// A distinct reactor per closed marker pair, registered explicitly rather than through assembly discovery.
/// </summary>
/// <typeparam name="TLeft">The first identity marker.</typeparam>
/// <typeparam name="TRight">The second identity marker.</typeparam>
public class RegistrationReactor<TLeft, TRight> : IReactor
{
    /// <summary>
    /// Handles the shared event type; the registration benchmark does not append events.
    /// </summary>
    /// <param name="event">The event to handle.</param>
    /// <returns>A completed task.</returns>
    public Task Handle(BenchmarkEvent @event)
    {
        _ = @event;
        return Task.CompletedTask;
    }
}
