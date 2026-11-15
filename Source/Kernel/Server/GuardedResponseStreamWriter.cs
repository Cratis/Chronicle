// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Services;
using Grpc.Core;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Writes responses only while the call has no recorded failure.
/// </summary>
/// <typeparam name="T">The response type.</typeparam>
/// <param name="inner">The response stream writer.</param>
/// <param name="context">The call context.</param>
internal sealed class GuardedResponseStreamWriter<T>(IServerStreamWriter<T> inner, ServerCallContext context) : IServerStreamWriter<T>
{
    /// <inheritdoc/>
    public WriteOptions? WriteOptions
    {
        get => inner.WriteOptions;
        set => inner.WriteOptions = value;
    }

    /// <inheritdoc/>
    public async Task WriteAsync(T message)
    {
        context.ThrowRecordedFailure();
        await inner.WriteAsync(message);
        context.ThrowRecordedFailure();
    }
}
