// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ServiceModel;
using ProtoBuf.Grpc;

namespace Cratis.Events.Store.Grpc.Contracts
{
    /// <summary>
    /// Defines the Grpc service contract for working with an event log.
    /// </summary>
    [ServiceContract]
    public interface IEventLogService
    {
        /// <summary>
        /// Append an event to a specific event log.
        /// </summary>
        /// <param name="request">The <see cref="AppendRequest">request</see> payload</param>
        /// <param name="context">Grpc <see cref="CallContext"/>.</param>
        /// <returns>The <see cref="AppendResult">result</see> of the append.</returns>
        ValueTask<AppendResult> Append(AppendRequest request, CallContext context = default);
    }
}
