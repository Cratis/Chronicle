// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Reducers;

/// <summary>
/// Represents an event and its context.
/// </summary>
/// <param name="Event">The content of the vent.</param>
/// <param name="Context"><see cref="EventContext"/> for the event.</param>
public record EventAndContext(object Event, EventContext Context);
