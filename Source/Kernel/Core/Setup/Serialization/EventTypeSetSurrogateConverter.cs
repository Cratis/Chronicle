// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Converts between an <see cref="EventTypeSet"/> and its <see cref="EventTypeSetSurrogate"/>.
/// </summary>
/// <remarks>
/// Converting back goes through <see cref="EventTypeSet.From(IEnumerable{EventType}?)"/>, so a list that crossed a silo boundary,
/// or was copied for a grain call, is the silo's shared instance again.
/// </remarks>
[RegisterConverter]
public sealed class EventTypeSetSurrogateConverter : IConverter<EventTypeSet, EventTypeSetSurrogate>
{
    /// <inheritdoc/>
    public EventTypeSet ConvertFromSurrogate(in EventTypeSetSurrogate surrogate) => EventTypeSet.From(surrogate.EventTypes);

    /// <inheritdoc/>
    public EventTypeSetSurrogate ConvertToSurrogate(in EventTypeSet value) => new([.. value]);
}
