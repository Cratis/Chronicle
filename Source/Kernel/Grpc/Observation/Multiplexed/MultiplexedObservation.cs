// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Observation.Multiplexed;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Observation.Multiplexed;

/// <summary>
/// Represents an implementation of <see cref="IMultiplexedObservation"/>.
/// </summary>
/// <remarks>
/// Every stream of the connection is served by the same <see cref="IReactors"/> and <see cref="IReducers"/> a
/// per-observer call is, so registration, the registration gate, the subscription keeper, delivery and the
/// disconnect handling are the ones that already exist; this class only carries their messages over one stream.
/// </remarks>
/// <param name="reactors">The <see cref="IReactors"/> serving the reactor streams.</param>
/// <param name="reducers">The <see cref="IReducers"/> serving the reducer streams.</param>
/// <param name="logger">The <see cref="ILogger{T}"/> for logging.</param>
internal sealed class MultiplexedObservation(
    IReactors reactors,
    IReducers reducers,
    ILogger<MultiplexedObservation> logger) : IMultiplexedObservation
{
    /// <inheritdoc/>
    public IObservable<MultiplexedServerMessage> Observe(IObservable<MultiplexedClientMessage> messages, CallContext context = default) =>
        Observable.Create<MultiplexedServerMessage>(observer =>
        {
            logger.ConnectionOpened();
            var connection = new MultiplexedConnection(reactors, reducers, observer, context, logger);
            var subscription = messages.Subscribe(connection.Receive, connection.Fail, connection.Complete);
            var cancellation = context.CancellationToken.Register(connection.Complete);
            return () =>
            {
                cancellation.Dispose();
                subscription.Dispose();
                connection.Dispose();
                logger.ConnectionClosed();
            };
        });
}
