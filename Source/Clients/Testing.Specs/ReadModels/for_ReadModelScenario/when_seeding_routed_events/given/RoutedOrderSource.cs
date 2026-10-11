// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSources;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_routed_events.given;

/// <summary>
/// Declares the event source used to verify typed reducer filters.
/// </summary>
[EventSource("RoutedOrder")]
[EventStream("Payments")]
public record RoutedOrderSource : IEventSource;
