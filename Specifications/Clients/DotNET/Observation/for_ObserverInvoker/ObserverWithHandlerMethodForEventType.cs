// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Observation.for_ObserverInvoker;

public class ObserverWithHandlerMethodForEventType
{
    public Task Handle(MyEvent @event) => Task.CompletedTask;
}
