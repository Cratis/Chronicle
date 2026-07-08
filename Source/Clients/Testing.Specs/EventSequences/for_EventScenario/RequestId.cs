// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// A Guid-backed concept identifier used as one half of a composite unique-constraint key.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record RequestId(Guid Value) : ConceptAs<Guid>(Value);
