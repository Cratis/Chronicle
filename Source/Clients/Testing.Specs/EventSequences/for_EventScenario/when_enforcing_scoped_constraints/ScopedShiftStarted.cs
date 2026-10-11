// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_enforcing_scoped_constraints;

/// <summary>
/// Event constrained to once per event source and event stream by <see cref="OneShiftPerEmployeeAndStream"/>.
/// </summary>
/// <param name="Location">Where the shift is worked.</param>
[EventType]
public record ScopedShiftStarted(string Location);
