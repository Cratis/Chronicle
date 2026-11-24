// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Benchmarks.Kernel;

/// <summary>
/// Serves <see cref="IClientStreamsProbe"/>.
/// </summary>
public class ClientStreamsProbeService : IClientStreamsProbe
{
    /// <inheritdoc/>
    public async IAsyncEnumerable<ClientStreamsProbeMessage> Watch(ClientStreamsProbeRequest request, CallContext context = default)
    {
        yield return new ClientStreamsProbeMessage { Value = 1 };
        await Task.Delay(Timeout.Infinite, context.CancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }
}
