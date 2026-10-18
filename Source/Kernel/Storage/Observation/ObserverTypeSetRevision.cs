// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Represents a committed revision of a namespace observer's membership.
/// </summary>
/// <param name="Value">The revision.</param>
public record ObserverTypeSetRevision(long Value) : ConceptAs<long>(Value);
