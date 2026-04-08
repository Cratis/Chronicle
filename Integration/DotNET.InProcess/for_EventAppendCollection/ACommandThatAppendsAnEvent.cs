// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.InProcess.Integration.for_EventAppendCollection;

[Command]
public class ACommandThatAppendsAnEvent
{
    public EventSourceId EventSourceId { get; init; }

    async Task Handle(IEventLog eventLog) =>
        await eventLog.Append(EventSourceId, new ACommandHandledEvent());
}
