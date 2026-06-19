// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker;

public class ReactorWithHandlerMethodWithDependencies
{
    public Task Handle(MyEvent @event, EventContext context, DependencyReadModel readModel, IDependencyService service) => Task.CompletedTask;
}
