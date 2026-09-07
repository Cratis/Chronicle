// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.EventSequences;

namespace Cratis.Chronicle.Testing.for_InProcessCommandPipeline;

[Command]
public record AppendConsumerEvent(EventSourceId EventSourceId)
{
    public async Task Handle(EventScenario scenario)
    {
        var result = await scenario.EventLog.Append(EventSourceId, new TestEvent("from consumer"));
        result.ShouldBeSuccessful();
    }
}
