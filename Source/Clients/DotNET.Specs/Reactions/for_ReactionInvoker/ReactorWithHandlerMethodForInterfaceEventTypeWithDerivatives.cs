// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker;

public class ReactorWithHandlerMethodForInterfaceEventTypeWithDerivatives
{
    public Task Handle(IMyEvent @event) => Task.CompletedTask;
}
