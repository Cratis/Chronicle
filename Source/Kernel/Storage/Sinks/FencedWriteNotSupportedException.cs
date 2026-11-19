// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Exception that gets thrown when a <see cref="SinkWriteMode.FencedGlobal"/> write would need something a fenced write cannot protect.
/// </summary>
/// <remarks>
/// Thrown before any read model content is written, so the partition fails visibly instead of being written without protection.
/// </remarks>
/// <param name="reason">Why the write cannot be fenced.</param>
public class FencedWriteNotSupportedException(string reason) : Exception($"A globally scoped projection write cannot be fenced: {reason}")
{
    /// <summary>
    /// Gets why the write cannot be fenced.
    /// </summary>
    public string Reason { get; } = reason;
}
