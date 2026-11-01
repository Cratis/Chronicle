// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Registrations;

/// <summary>
/// Represents a per-observer compare-and-set revision; zero denotes an absent registration.
/// </summary>
/// <param name="Value">The revision.</param>
public record ObserverRegistrationRevision(ulong Value) : ConceptAs<ulong>(Value);
