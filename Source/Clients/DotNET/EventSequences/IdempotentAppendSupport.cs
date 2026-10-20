// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Clients;
using Grpc.Core;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Validates idempotent append support. A keyed append is never sent unkeyed, so an absent or unknown capability refuses.
/// </summary>
internal static class IdempotentAppendSupport
{
    /// <summary>
    /// Gets whether the connection advertises idempotent appends.
    /// </summary>
    /// <param name="connection">The current kernel connection.</param>
    /// <returns>True when the capability is known and advertised.</returns>
    internal static bool IsSupported(IChronicleConnection connection) =>
        connection is IKernelCapabilities { CapabilitiesAreKnown: true } capabilities &&
        capabilities.Capabilities.Contains(KernelCapabilities.IdempotentAppends);

    /// <summary>
    /// Requires an explicitly advertised idempotent append capability.
    /// </summary>
    /// <param name="connection">The current kernel connection.</param>
    /// <exception cref="IdempotentAppendsNotSupported">The capability is absent or unknown.</exception>
    internal static void Require(IChronicleConnection connection)
    {
        if (!IsSupported(connection))
        {
            throw new IdempotentAppendsNotSupported();
        }
    }

    /// <summary>
    /// Runs a keyed call, turning an Unimplemented reply from a kernel without the RPC into <see cref="IdempotentAppendsNotSupported"/>.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="call">The call to run.</param>
    /// <returns>The call's result.</returns>
    /// <exception cref="IdempotentAppendsNotSupported">The kernel answered Unimplemented; nothing was appended.</exception>
    internal static async Task<TResult> Wrap<TResult>(Func<Task<TResult>> call)
    {
        try
        {
            return await call();
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.Unimplemented)
        {
            throw new IdempotentAppendsNotSupported();
        }
    }
}
