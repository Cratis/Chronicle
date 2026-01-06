// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.ReadModels;

/// <summary>
/// Represents an event that led to a read model snapshot.
/// </summary>
/// <param name="SequenceNumber">The sequence number of the event.</param>
/// <param name="Type">The type of the event.</param>
/// <param name="Occurred">When the event occurred.</param>
/// <param name="Content">The content of the event.</param>
public record Event(ulong SequenceNumber, string Type, DateTimeOffset Occurred, object Content);
