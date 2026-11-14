// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Represents a slot of the applied event markers and the event it records.
/// </summary>
/// <param name="Path">The path of the slot.</param>
/// <param name="Stream">The identity of the event stream the event belongs to.</param>
/// <param name="SequenceNumber">The event sequence number of the event.</param>
internal sealed record AppliedEventMarkerSlot(string Path, string Stream, long SequenceNumber);
