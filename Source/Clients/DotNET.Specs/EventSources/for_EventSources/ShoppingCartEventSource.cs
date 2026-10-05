// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSources.for_EventSources;

[EventSource("ShoppingCart", Description = "A cart", Concurrency = ConcurrencyDimensions.EventSourceId)]
[EventStream("Items", Description = "The items", Concurrency = ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType)]
[EventStream("Payment")]
public class ShoppingCartEventSource : IEventSource;
