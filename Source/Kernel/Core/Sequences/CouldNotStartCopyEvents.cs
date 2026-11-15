// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Exception that gets thrown when the job copying events between event sources could not be started.
/// </summary>
/// <param name="sourceId">The event source being copied from.</param>
/// <param name="targetId">The event source being copied to.</param>
public class CouldNotStartCopyEvents(EventSourceId sourceId, EventSourceId targetId)
    : Exception($"Could not start the job copying events from '{sourceId}' to '{targetId}'");
