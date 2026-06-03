// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.for_EventAppendCollection;

[DependencyInjection.IgnoreConvention]
public class AReactorThatExecutesACommand(ICommandPipeline commandPipeline, IServiceProvider serviceProvider) : IReactor
{
    public Task OnAnEventHappened(AnEventHappened evt, EventContext ctx) =>
        commandPipeline.Execute(
            new ACommandThatAppendsAnEvent { EventSourceId = ctx.EventSourceId },
            serviceProvider);
}
