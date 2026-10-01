// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Represents what the kernel knew about the failure when it recorded an alert transition.
/// </summary>
/// <remarks>
/// The message is the first message of the latest failure, cut to <see cref="MaxMessageLength"/> characters. The
/// exception type is not recorded. Use <see cref="Create"/> to get the message cut; the constructor takes it as given.
/// </remarks>
/// <param name="AttemptCount">How many times the partition has failed.</param>
/// <param name="FirstFailure">When the partition first failed.</param>
/// <param name="LastFailure">When the partition last failed.</param>
/// <param name="FailureKind">What kind of thing went wrong on the latest failure.</param>
/// <param name="Message">The message of the latest failure, at most <see cref="MaxMessageLength"/> characters.</param>
public record AlertEvidence(
    int AttemptCount,
    DateTimeOffset FirstFailure,
    DateTimeOffset LastFailure,
    FailureKind FailureKind,
    string Message)
{
    /// <summary>
    /// The maximum number of characters kept of a failure message.
    /// </summary>
    public const int MaxMessageLength = 200;

    /// <summary>
    /// Creates an <see cref="AlertEvidence"/> with the message cut to <see cref="MaxMessageLength"/> characters.
    /// </summary>
    /// <param name="attemptCount">How many times the partition has failed.</param>
    /// <param name="firstFailure">When the partition first failed.</param>
    /// <param name="lastFailure">When the partition last failed.</param>
    /// <param name="failureKind">What kind of thing went wrong on the latest failure.</param>
    /// <param name="message">The message of the latest failure.</param>
    /// <returns>The <see cref="AlertEvidence"/>.</returns>
    public static AlertEvidence Create(
        int attemptCount,
        DateTimeOffset firstFailure,
        DateTimeOffset lastFailure,
        FailureKind failureKind,
        string message) =>
        new(attemptCount, firstFailure, lastFailure, failureKind, Truncate(message));

    static string Truncate(string message)
    {
        if (message.Length <= MaxMessageLength)
        {
            return message;
        }

        // Do not cut a surrogate pair in half.
        var length = char.IsHighSurrogate(message[MaxMessageLength - 1]) ? MaxMessageLength - 1 : MaxMessageLength;
        return message[..length];
    }
}
