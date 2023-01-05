// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.EventSequences;

/// <summary>
/// Extension methods for <see cref="StreamSequenceToken"/>.
/// </summary>
public static class StreamSequenceTokenExtensions
{
    /// <summary>
    /// Check if a token represents a warm up event.
    /// </summary>
    /// <param name="token"><see cref="StreamSequenceToken"/> to check.</param>
    /// <returns>True if it is a warm up event, false if not.</returns>
    public static bool IsWarmUp(this StreamSequenceToken token) => token.SequenceNumber == (long)EventSequenceNumber.WarmUp.Value;
}
