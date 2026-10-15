// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Identifies the event stream protected by a decision read.
/// </summary>
/// <param name="EventSourceId">The source identifier, independent of the read model key.</param>
/// <param name="EventStreamType">The stream type.</param>
/// <param name="EventStreamId">The stream identifier.</param>
/// <param name="EventSourceType">The optional source type.</param>
internal sealed record DecisionReadStreamScope(
    EventSourceId EventSourceId,
    EventStreamType EventStreamType,
    EventStreamId EventStreamId,
    EventSourceType? EventSourceType);
