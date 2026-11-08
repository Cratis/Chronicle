// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.CodeGeneration;
using Orleans.Concurrency;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Defines the grain that pulls one binding's producer outbox into the local inbox. The key is the binding id, so there is one activation per binding in the cluster.
/// </summary>
[Version(1)]
public interface IFederationConsumer : IGrainWithStringKey
{
    /// <summary>
    /// Make sure the consumer is running. Idempotent and interleaved, so it never waits for a pull in progress.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    [AlwaysInterleave]
    Task Ensure();

    /// <summary>
    /// Stop the consumer, for instance because its binding was removed. Interleaved, so it never waits for a pull in progress.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    [AlwaysInterleave]
    Task Stop();
}
