// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Grains.EventSequences.for_AppendedEventsQueue;

public record HandledEvents(Key Partition, IEnumerable<AppendedEvent> Events);
