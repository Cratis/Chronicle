// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Connections.Multiplexed;

/// <summary>
/// Represents an <see cref="IReactors"/> that carries observation over a shared <see cref="MultiplexedObservationChannel"/>.
/// </summary>
/// <param name="inner">The <see cref="IReactors"/> that serves everything but observation.</param>
/// <param name="channel">The <see cref="MultiplexedObservationChannel"/> carrying the observation.</param>
public sealed class MultiplexedReactors(IReactors inner, MultiplexedObservationChannel channel) : IReactors
{
    /// <inheritdoc/>
    public IObservable<EventsToObserve> Observe(IObservable<ReactorMessage> messages, CallContext context = default) =>
        channel.ObserveReactor(messages, context.CancellationToken);

    /// <inheritdoc/>
    public Task<HasReactorResponse> HasReactor(HasReactorRequest request, CallContext context = default) => inner.HasReactor(request, context);
}
