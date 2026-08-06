// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// Event constrained to at most one open shift per employee by the fluent <see cref="OneOpenShiftPerEmployee"/>.
/// </summary>
/// <param name="Location">Where the shift is worked.</param>
[EventType]
public record ShiftStarted(string Location);
