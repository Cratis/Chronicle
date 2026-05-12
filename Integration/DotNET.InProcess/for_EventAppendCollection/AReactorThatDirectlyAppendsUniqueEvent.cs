// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.InProcess.Integration.for_EventAppendCollection;

[DependencyInjection.IgnoreConvention]
public class AReactorThatDirectlyAppendsUniqueEvent(IEventLog eventLog) : IReactor
{
    public Task OnADirectUniqueEvent(ADirectUniqueEvent evt, EventContext ctx) =>
        eventLog.Append(ctx.EventSourceId, new ADirectUniqueFollowUpEvent(evt.UniqueValue));
}
