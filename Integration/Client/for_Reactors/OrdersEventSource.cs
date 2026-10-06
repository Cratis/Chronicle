// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSources;

namespace Cratis.Chronicle.Integration.for_Reactors;

[EventSource("Orders")]
public class OrdersEventSource : IEventSource;
