// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverStateStorage.when_querying_retired;

public class and_candidates_have_different_dispositions : Specification
{
    ObserverStateStorage _storage;
    IEnumerable<ObserverId> _result;

    async Task Establish()
    {
        _storage = new();
        await _storage.Save(new ObserverState { Identifier = "retired", AlertDisposition = AlertDisposition.Retired });
        await _storage.Save(new ObserverState { Identifier = "active", AlertDisposition = AlertDisposition.Active });
        await _storage.Save(new ObserverState { Identifier = "unsubmitted", AlertDisposition = AlertDisposition.Retired });
    }

    async Task Because() => _result = await _storage.GetRetired(["retired", "active", "missing", "retired"]);

    [Fact] void should_only_return_the_submitted_retired_observer() => _result.ShouldContainOnly((ObserverId)"retired");

    void Destroy() => _storage.Dispose();
}
