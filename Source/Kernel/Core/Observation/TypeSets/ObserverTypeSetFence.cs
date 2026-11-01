// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.TypeSets;

/// <summary>
/// Represents a join fence. A wrapper keeps zero distinct from no fence on the wire.
/// </summary>
/// <param name="Value">The event sequence number fence.</param>
public record ObserverTypeSetFence(ulong Value);
