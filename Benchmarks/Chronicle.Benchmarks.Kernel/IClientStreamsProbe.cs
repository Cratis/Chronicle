// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace Cratis.Chronicle.Benchmarks.Kernel;

/// <summary>
/// The server-streaming call the client stream benchmark opens.
/// </summary>
[Service]
public interface IClientStreamsProbe
{
    /// <summary>
    /// Sends one message and then keeps the stream open until the client cancels it.
    /// </summary>
    /// <param name="request">The <see cref="ClientStreamsProbeRequest"/>.</param>
    /// <param name="context">The call context.</param>
    /// <returns>The messages of the stream.</returns>
    IAsyncEnumerable<ClientStreamsProbeMessage> Watch(ClientStreamsProbeRequest request, CallContext context = default);
}
