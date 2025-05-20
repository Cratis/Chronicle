// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Events;

/// <summary>
/// Represents the metadata related to an event.
/// </summary>
/// <param name="SequenceNumber">The sequence number of the event.</param>
/// <param name="Type">The type of the event.</param>
public record EventMetadata(
    ulong SequenceNumber,
    EventType Type);
