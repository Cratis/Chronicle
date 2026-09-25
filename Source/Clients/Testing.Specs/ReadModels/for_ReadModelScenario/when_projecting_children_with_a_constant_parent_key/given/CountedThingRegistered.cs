// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_children_with_a_constant_parent_key.given;

/// <summary>
/// A thing came into existence in a state worth counting.
/// </summary>
/// <param name="Label">A value the projection reads off the event content.</param>
/// <param name="IsOpen">Whether the thing starts out open.</param>
[EventType]
public record CountedThingRegistered(string Label, bool IsOpen);
