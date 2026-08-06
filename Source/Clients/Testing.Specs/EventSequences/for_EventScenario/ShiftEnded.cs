// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// Ends the shift, releasing the <see cref="OneOpenShiftPerEmployee"/> constraint so the employee can start the next one.
/// </summary>
[EventType]
public record ShiftEnded;
