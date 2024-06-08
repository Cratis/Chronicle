// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Represents a <see cref="IParticipateInConnectionLifecycle"/> for handling <see cref="IReducersRegistrar"/>.
/// </summary>
public class RegistrarsConnectionLifecycleParticipant : IParticipateInConnectionLifecycle
{
    readonly IReducersRegistrar _reducers;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegistrarsConnectionLifecycleParticipant"/> class.
    /// </summary>
    /// <param name="reducers"><see cref="IReducersRegistrar"/> to work with.</param>
    public RegistrarsConnectionLifecycleParticipant(IReducersRegistrar reducers) => _reducers = reducers;

    /// <inheritdoc/>
    public Task ClientConnected() => _reducers.Initialize();

    /// <inheritdoc/>
    public Task ClientDisconnected() => Task.CompletedTask;
}
