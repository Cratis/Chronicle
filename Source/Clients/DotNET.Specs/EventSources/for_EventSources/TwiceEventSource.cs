// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSources.for_EventSources;

[EventSource("Twice")]
[EventStream("Items")]
[EventStream("Items")]
public class TwiceEventSource : IEventSource;
