// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>
/// What one benchmark invocation did. Only successful work counts as operations and events; failures are counted
/// separately so a failed write can never make a result look like a success.
/// </summary>
/// <param name="Operations">The operations that succeeded.</param>
/// <param name="Events">The events the successful operations carried or returned.</param>
/// <param name="Commands">The commands the invocation caused.</param>
/// <param name="Latencies">The latency of each successful operation.</param>
/// <param name="Errors">The operations that failed.</param>
public sealed record InvocationRecord(long Operations, long Events, CommandSnapshot Commands, IReadOnlyList<TimeSpan> Latencies, long Errors)
{
    /// <summary>Gets a record of one failed operation that counts no work.</summary>
    public static InvocationRecord Failure { get; } = new(0, 0, new(0, new Dictionary<string, long>()), [], 1);
}
