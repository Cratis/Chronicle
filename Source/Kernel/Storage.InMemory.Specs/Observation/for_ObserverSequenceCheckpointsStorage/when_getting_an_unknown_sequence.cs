// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverSequenceCheckpointsStorage;

public class when_getting_an_unknown_sequence : Specification
{
    ObserverSequenceCheckpoint _result;

    async Task Because()
    {
        var storage = new ObserverSequenceCheckpointsStorage();
        await storage.Save(ObserverSequenceCheckpoint.Fresh("observer", "A"));
        _result = await storage.Get("observer", "B");
    }

    [Fact] void should_not_invent_a_checkpoint() => _result.ShouldBeNull();
}
