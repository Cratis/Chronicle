// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation;

/// <summary>
/// Carries optional persisted context for an abandoned client delivery, without changing the wire contract.
/// </summary>
/// <param name="ReleaseBy">The latest time the abandoned call is held.</param>
/// <param name="Targets">The original delivery targets, never replacement subscription targets.</param>
public record DeliveryFenceContext(DateTimeOffset? ReleaseBy, IReadOnlyList<DeliveryFenceTarget>? Targets);
