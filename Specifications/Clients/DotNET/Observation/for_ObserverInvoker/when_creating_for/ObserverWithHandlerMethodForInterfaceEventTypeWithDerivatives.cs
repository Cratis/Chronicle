// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



namespace Aksio.Cratis.Observation.for_ObserverInvoker.when_creating_for;

public class ObserverWithHandlerMethodForInterfaceEventTypeWithDerivatives
{
    public Task Handle(IMyEvent @event) => Task.CompletedTask;
}
