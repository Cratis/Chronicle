// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.MongoDB.Changes;

/// <summary>
/// Represents the unique key for a model change.
/// </summary>
/// <param name="Key">The model key.</param>
/// <param name="SequenceNumber">The sequence number of the event that caused the change.</param>
/// <param name="CorrelationId">CorrelationId of the change.</param>
public record ModelChangeKey(string Key, EventSequenceNumber SequenceNumber, CorrelationId CorrelationId);
