// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event that sets a work arrangement; its <see cref="Location"/> is the explicit source for the
/// collided read-model property.
/// </summary>
/// <param name="Location">The work location.</param>
/// <param name="WorkMode">The work mode.</param>
[EventType]
public record WorkArrangementSet(string Location, int WorkMode);
