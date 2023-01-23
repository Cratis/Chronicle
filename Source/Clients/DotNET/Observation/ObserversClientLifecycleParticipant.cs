// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Clients;

namespace Aksio.Cratis.Observation;

/// <summary>
/// Represents a <see cref="IParticipateInClientLifecycle"/> for handling <see cref="IObservers"/>.
/// </summary>
public class ObserversClientLifecycleParticipant : IParticipateInClientLifecycle
{
    readonly IObservers _observers;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserversClientLifecycleParticipant"/> class.
    /// </summary>
    /// <param name="observers"><see cref="IObservers"/> to work with.</param>
    public ObserversClientLifecycleParticipant(IObservers observers) => _observers = observers;

    /// <inheritdoc/>
    public Task ClientConnected() => _observers.Initialize();

    /// <inheritdoc/>
    public Task ClientDisconnected() => Task.CompletedTask;
}
