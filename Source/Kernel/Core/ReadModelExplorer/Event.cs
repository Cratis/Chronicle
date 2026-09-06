// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Sequences;

namespace Cratis.Chronicle.ReadModelExplorer;

/// <summary>
/// Represents an event that led to a read model snapshot.
/// </summary>
/// <param name="Context">The context the event was appended with.</param>
/// <param name="Content">The JSON representation of the event's content.</param>
/// <remarks>
/// This carries the same shape as <see cref="AppendedEvent"/> rather than a flattened summary of it, because a
/// snapshot's events are what a client rehydrates its own events from - it needs the whole context, not the few
/// values a viewer happens to display. It cannot simply be <see cref="AppendedEvent"/>: that is a read model in
/// its own right, and a read model is a query's result rather than something another read model can hold.
/// </remarks>
public record Event(EventContext Context, string Content);
