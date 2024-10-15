// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Aggregates.for_AggregateRootExtensions.when_getting_event_stream_type;

[EventStreamType(EventStreamType)]
public class AggregateRootWithEventStreamType : AggregateRoot
{
    public const string EventStreamType = "SomeEventStreamType";
}
