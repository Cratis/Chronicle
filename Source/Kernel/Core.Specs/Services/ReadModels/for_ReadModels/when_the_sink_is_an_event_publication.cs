// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels;

public class when_the_sink_is_an_event_publication : when_the_database_is_marked
{
    void Establish()
    {
        _readModelDefinition = _readModelDefinition with
        {
            Sink = new(SinkConfigurationId.None, WellKnownSinkTypes.EventSequence)
            {
                EventSequence = new(new EventType("Published", EventTypeGeneration.First), "publication")
            }
        };
        _readModel.GetDefinition().Returns(_readModelDefinition);
    }
}
