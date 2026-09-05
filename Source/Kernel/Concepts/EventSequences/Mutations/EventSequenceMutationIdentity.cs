// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Concepts.EventSequences.Mutations;

/// <summary>
/// Represents the display form and canonical strict UTF-8 key of an event sequence mutation identity.
/// </summary>
public sealed record EventSequenceMutationIdentity
{
    const int MaxUtf16CodeUnits = 200;
    const int MaxUtf8Bytes = 600;
    static readonly UTF8Encoding _strictUtf8 = new(false, true);

    EventSequenceMutationIdentity(string display, EventSequenceIdentityKey key)
    {
        Display = display;
        Key = key;
    }

    /// <summary>
    /// Gets the exact, unmodified display form of the event sequence identifier.
    /// </summary>
    public string Display { get; }

    /// <summary>
    /// Gets the canonical strict UTF-8 key for the display form.
    /// </summary>
    public EventSequenceIdentityKey Key { get; }

    /// <summary>
    /// Tries to create an identity without normalization, case folding, trimming, or replacement.
    /// </summary>
    /// <param name="display">The event sequence identifier display form.</param>
    /// <returns>A result containing the identity or a typed reason why it is unsupported.</returns>
    public static EventSequenceMutationIdentityCreationResult TryCreate(string? display)
    {
        if (display is null)
        {
            return EventSequenceMutationIdentityCreationResult.Failed(UnsupportedEventSequenceIdReason.MissingValue);
        }

        if (display.Contains('\0', StringComparison.Ordinal))
        {
            return EventSequenceMutationIdentityCreationResult.Failed(UnsupportedEventSequenceIdReason.ContainsNul);
        }

        if (display.Length > MaxUtf16CodeUnits)
        {
            return EventSequenceMutationIdentityCreationResult.Failed(UnsupportedEventSequenceIdReason.TooLong);
        }

        byte[] bytes;
        try
        {
            bytes = _strictUtf8.GetBytes(display);
        }
        catch (EncoderFallbackException)
        {
            return EventSequenceMutationIdentityCreationResult.Failed(UnsupportedEventSequenceIdReason.IllFormedUtf16);
        }

        if (bytes.Length > MaxUtf8Bytes)
        {
            return EventSequenceMutationIdentityCreationResult.Failed(UnsupportedEventSequenceIdReason.TooLong);
        }

        try
        {
            var decoded = _strictUtf8.GetString(bytes);
            var reencoded = _strictUtf8.GetBytes(decoded);
            if (!string.Equals(display, decoded, StringComparison.Ordinal) || !bytes.AsSpan().SequenceEqual(reencoded))
            {
                return EventSequenceMutationIdentityCreationResult.Failed(UnsupportedEventSequenceIdReason.IllFormedUtf16);
            }
        }
        catch (DecoderFallbackException)
        {
            return EventSequenceMutationIdentityCreationResult.Failed(UnsupportedEventSequenceIdReason.IllFormedUtf16);
        }

        return EventSequenceMutationIdentityCreationResult.Succeeded(new(display, new EventSequenceIdentityKey(bytes)));
    }
}
