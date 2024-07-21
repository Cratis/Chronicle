// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactions.for_ObserverInvoker;

public class ReactionWithHandlerMethodForInterfaceEventTypeWithDerivatives
{
    public Task Handle(IMyEvent @event) => Task.CompletedTask;
}
