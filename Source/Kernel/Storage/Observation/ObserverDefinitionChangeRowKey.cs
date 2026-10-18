// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Identifies a row within an observer revision. Progress uses the job and partition; continuations use the partition;
/// restart plans use the plan id; inventory uses the job id. Providers retain the full identity in the payload.
/// </summary>
/// <param name="Observer">The complete observer key, including the event sequence.</param>
/// <param name="Revision">The revision that owns the row, including an unpublished successor revision.</param>
/// <param name="Id">The stable identity within this row family and revision.</param>
public record ObserverDefinitionChangeRowKey(ObserverKey Observer, ObserverTypeSetRevision Revision, string Id);
