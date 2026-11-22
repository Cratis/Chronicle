// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Registrations;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// Holds log messages for <see cref="RegistrationInbox"/>.
/// </summary>
internal static partial class RegistrationInboxLogging
{
    [LoggerMessage(LogLevel.Information, "Accepted {Kind} registration revision {Revision} for event store {EventStore} from '{Registrant}'")]
    internal static partial void Accepted(this ILogger<RegistrationInbox> logger, EventStoreName eventStore, RegistrationKind kind, long revision, string registrant);

    [LoggerMessage(LogLevel.Debug, "Registration from '{Registrant}' for event store {EventStore} repeats revision {Revision}; nothing to accept")]
    internal static partial void Unchanged(this ILogger<RegistrationInbox> logger, EventStoreName eventStore, string registrant, long revision);

    [LoggerMessage(LogLevel.Information, "Applied {Kind} registration revision {Revision} for event store {EventStore} from '{Registrant}'")]
    internal static partial void Applied(this ILogger<RegistrationInbox> logger, EventStoreName eventStore, RegistrationKind kind, long revision, string registrant);

    [LoggerMessage(LogLevel.Warning, "Applying {Kind} registration revision {Revision} for event store {EventStore} from '{Registrant}' failed on attempt {Attempt} of {MaxAttempts}; trying again")]
    internal static partial void ApplyFailedWillRetry(this ILogger<RegistrationInbox> logger, Exception exception, EventStoreName eventStore, RegistrationKind kind, long revision, string registrant, int attempt, int maxAttempts);

    [LoggerMessage(LogLevel.Error, "The accepted {Kind} registration revision {Revision} for event store {EventStore} from '{Registrant}' could not be applied after {Attempts} attempts and is recorded as failed. Register again to retry it")]
    internal static partial void ApplyFailed(this ILogger<RegistrationInbox> logger, Exception exception, EventStoreName eventStore, RegistrationKind kind, long revision, string registrant, int attempts);

    [LoggerMessage(LogLevel.Error, "Could not record the outcome of registration revision {Revision} for event store {EventStore}; it stays pending in memory")]
    internal static partial void RecordingOutcomeFailed(this ILogger<RegistrationInbox> logger, Exception exception, EventStoreName eventStore, long revision);

    [LoggerMessage(LogLevel.Information, "Event store {EventStore} has {Count} accepted registration(s) waiting to be applied")]
    internal static partial void ResumingPending(this ILogger<RegistrationInbox> logger, EventStoreName eventStore, int count);

    [LoggerMessage(LogLevel.Debug, "The storage provider of event store {EventStore} cannot keep registration intents; registrations are applied before they are answered")]
    internal static partial void IntentsNotSupported(this ILogger<RegistrationInbox> logger, EventStoreName eventStore);
}
