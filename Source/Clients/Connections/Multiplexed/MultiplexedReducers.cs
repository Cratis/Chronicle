// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reducers;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Connections.Multiplexed;

/// <summary>
/// Represents an <see cref="IReducers"/> that carries observation over a shared <see cref="MultiplexedObservationChannel"/>.
/// </summary>
/// <param name="channel">The <see cref="MultiplexedObservationChannel"/> carrying the observation.</param>
public sealed class MultiplexedReducers(MultiplexedObservationChannel channel) : IReducers
{
    /// <inheritdoc/>
    public IObservable<ReduceOperationMessage> Observe(IObservable<ReducerMessage> messages, CallContext context = default) =>
        channel.ObserveReducer(messages, context.CancellationToken);
}
