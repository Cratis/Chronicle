// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.ReadModels.for_DecisionReadExtensions;

public class when_mapping_guarded_append_conflicts : Specification
{
    IEnumerable<DecisionConflict> _conflicts;

    void Because()
    {
        var result = new AppendManyResult { ConcurrencyViolations = [new ConcurrencyViolation("source", 24, 27)] };
        IDecisionRead[] reads =
        [
            new DecisionRead<object>("source", null, "store", "namespace", 24, [new EventType("created", 1)]),
            new DecisionRead<object>("unrelated", null, "store", "namespace", 24, [new EventType("created", 1)])
        ];
        _conflicts = result.GetDecisionConflicts(reads);
    }

    [Fact] void should_map_only_the_violated_read() => _conflicts.ShouldContainOnly([new DecisionConflict(typeof(object), "source")]);
}
