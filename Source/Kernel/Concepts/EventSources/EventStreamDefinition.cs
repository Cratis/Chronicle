// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.EventSources;

/// <summary>
/// Represents the definition of an event stream belonging to an event source.
/// </summary>
/// <param name="Name">The name of the stream, which becomes the <see cref="EventStreamType"/> of appended events.</param>
/// <param name="Description">The description of the stream.</param>
/// <param name="Concurrency">The <see cref="ConcurrencyDimensions"/> taking part in concurrency checks for the stream.</param>
public record EventStreamDefinition(
    EventStreamType Name,
    EventStreamDescription Description,
    ConcurrencyDimensions Concurrency);
