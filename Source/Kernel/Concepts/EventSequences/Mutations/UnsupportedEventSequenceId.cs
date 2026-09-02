// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.EventSequences.Mutations;

/// <summary>
/// The exception that is thrown when an event sequence identifier is unsupported by mutation contracts.
/// </summary>
/// <param name="display">The unsupported display value.</param>
/// <param name="reason">The typed reason the value is unsupported.</param>
public class UnsupportedEventSequenceId(string? display, UnsupportedEventSequenceIdReason reason) :
    Exception($"The event sequence identifier '{display ?? "<missing>"}' is unsupported: {reason}.")
{
    /// <summary>
    /// Gets the typed reason the identifier is unsupported.
    /// </summary>
    public UnsupportedEventSequenceIdReason Reason { get; } = reason;
}
