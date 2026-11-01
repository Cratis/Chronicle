// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.TypeSets;

/// <summary>
/// Represents a member incarnation. A wrapper keeps zero distinct from no incarnation on the wire.
/// </summary>
/// <param name="Value">The incarnation.</param>
public record ObserverTypeSetIncarnation(long Value);
