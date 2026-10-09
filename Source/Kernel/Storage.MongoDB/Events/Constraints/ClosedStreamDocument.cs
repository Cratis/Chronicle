// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints;

/// <summary>
/// Represents a MongoDB document for an owned closed scope, including legacy stream-only documents.
/// </summary>
/// <param name="StreamType">The stream type.</param>
/// <param name="StreamId">The stream identifier.</param>
/// <param name="EventSourceId">The event source identifier.</param>
/// <param name="EventSourceType">The event source type.</param>
/// <param name="Owner">The owner; null on legacy manual closures.</param>
/// <param name="Dimensions">The scope mask; null on legacy stream-only closures.</param>
/// <param name="SequenceNumber">The closing sequence number.</param>
/// <param name="ClosedAt">The closing timestamp.</param>
public record ClosedStreamDocument(
    string? StreamType,
    string? StreamId,
    string? EventSourceId = default,
    string? EventSourceType = default,
    string? Owner = default,
    int? Dimensions = default,
    ulong? SequenceNumber = default,
    DateTimeOffset? ClosedAt = default);
