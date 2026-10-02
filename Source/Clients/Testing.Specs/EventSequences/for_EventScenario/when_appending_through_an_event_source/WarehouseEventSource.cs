// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSources;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_appending_through_an_event_source;

[EventSource("Warehouse")]
[EventStream("Receiving")]
public class WarehouseEventSource : IEventSource;
