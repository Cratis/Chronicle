// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting;

public class when_a_lowercase_guid_reads_uppercase_history : Specification
{
    EventStoreForTesting _store;
    DecisionRead<SimpleModule> _read;

    async Task Establish()
    {
        _store = new EventStoreForTesting();
        var upper = Guid.NewGuid().ToString("D").ToUpperInvariant();
        await _store.EventLog.Append((EventSourceId)upper, new ModuleCreated("Aliased"));
        _key = upper.ToLowerInvariant();
    }

    string _key;

    async Task Because() => _read = await _store.GetDecisionReads().GetDetached<SimpleModule>((ReadModelKey)_key);

    [Fact] void should_not_find_the_alias_history() => _read.Exists.ShouldBeFalse();
}
