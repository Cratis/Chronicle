// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Aggregates.for_AggregateRootEventHandlers.when_creating_for;

public class AggregateRootWithHandlerMethodForInterfaceEventTypeWithDerivatives : AggregateRoot
{
    public Task Handle(IMyEvent @event) => Task.CompletedTask;
}
