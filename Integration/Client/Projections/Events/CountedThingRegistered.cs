// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.Projections.Events;

/// <summary>
/// A thing came into existence in a state worth counting.
/// </summary>
/// <param name="Label">A label, so a set that carries values can be told apart from one that only carries membership.</param>
/// <param name="IsOpen">Whether the thing starts out open.</param>
[EventType]
public record CountedThingRegistered(string Label, bool IsOpen);
