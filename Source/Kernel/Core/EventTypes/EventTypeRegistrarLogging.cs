// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventTypes;

internal static partial class EventTypeRegistrarLogging
{
    [LoggerMessage(LogLevel.Warning, "Protection metadata added to event type {EventTypeId} generation {Generation} at {PropertyPath}. Earlier events remain plaintext and must be redacted if required.")]
    internal static partial void ComplianceMetadataAddedToExistingGeneration(this ILogger<EventTypeRegistrar> logger, string eventTypeId, uint generation, string propertyPath);
}
