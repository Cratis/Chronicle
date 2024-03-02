// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Connections;

namespace Aksio.Cratis.Observation;

/// <summary>
/// Represents a <see cref="IParticipateInConnectionLifecycle"/> for handling <see cref="IObserversRegistrar"/>.
/// </summary>
public class ObserversConnectionLifecycleParticipant : IParticipateInConnectionLifecycle
{
    readonly IObserversRegistrar _observers;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserversConnectionLifecycleParticipant"/> class.
    /// </summary>
    /// <param name="observers"><see cref="IObserversRegistrar"/> to work with.</param>
    public ObserversConnectionLifecycleParticipant(IObserversRegistrar observers) => _observers = observers;

    /// <inheritdoc/>
    public Task ClientConnected() => _observers.Initialize();

    /// <inheritdoc/>
    public Task ClientDisconnected() => Task.CompletedTask;
}
