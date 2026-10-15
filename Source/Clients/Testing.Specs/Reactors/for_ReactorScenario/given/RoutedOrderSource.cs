// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSources;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

/// <summary>
/// Declares the event source used to verify typed reactor filters.
/// </summary>
[EventSource("RouteOrder")]
[EventStream("Payments")]
public record RoutedOrderSource : IEventSource;
