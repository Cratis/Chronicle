// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// A Guid-backed concept value used by the EventScenario constraint specs.
/// </summary>
/// <param name="Value">The license key value.</param>
public record LicenseKey(Guid Value) : ConceptAs<Guid>(Value);
