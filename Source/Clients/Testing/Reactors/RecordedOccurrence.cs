// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// Represents a scenario event and its original context, which can be delivered repeatedly without allocating a new occurrence.
/// </summary>
/// <param name="Event">The recorded event instance.</param>
/// <param name="Context">The context allocated when the occurrence was recorded.</param>
public record RecordedOccurrence(object Event, EventContext Context);
