// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Holds log messages for <see cref="EventSequenceMutationDigestKeys"/>.
/// </summary>
internal static partial class EventSequenceMutationDigestKeysLogMessages
{
    [LoggerMessage(LogLevel.Warning, "The mutation digest key of event store '{EventStore}' could not be read or created. Mutations of the event store fail closed until it can")]
    internal static partial void DigestKeyUnavailable(this ILogger<EventSequenceMutationDigestKeys> logger, Exception exception, EventStoreName eventStore);
}
