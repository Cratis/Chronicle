// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Integration.for_EventAppendCollection;

[Command]
public record ACommandThatAppendsAnEvent
{
    public EventSourceId EventSourceId { get; init; }

    public async Task Handle(IEventLog eventLog) =>
        await eventLog.Append(EventSourceId, new ACommandHandledEvent());
}
