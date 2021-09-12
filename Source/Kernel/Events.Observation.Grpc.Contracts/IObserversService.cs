// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ServiceModel;
using ProtoBuf.Grpc;

namespace Cratis.Events.Observation.Grpc.Contracts
{
    /// <summary>
    /// Defines the Grpc service contract for observers.
    /// </summary>
    [ServiceContract]
    public interface IObserversService
    {
        /// <summary>
        /// Start subscribing based on the request payload.
        /// </summary>
        /// <param name="request"><see cref="IAsyncEnumerable{T}"/> of <see cref="ObserverClientToServer"/>.</param>
        /// <param name="context"><see cref="CallContext"/>.</param>
        /// <returns><see cref="IAsyncEnumerable{T}"/> of <see cref="ObserverServerToClient"/></returns>
        IAsyncEnumerable<ObserverServerToClient> Subscribe(IAsyncEnumerable<ObserverClientToServer> request, CallContext context = default);
    }
}
