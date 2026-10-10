// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.ReadModels.for_DecisionReadExtensions;

public class when_mapping_stream_scoped_conflicts : Specification
{
    IEnumerable<DecisionConflict> _conflicts;

    void Because()
    {
        var result = new AppendManyResult { ConcurrencyViolations = [new ConcurrencyViolation("source", 24, 27)] };
        IDecisionRead[] reads =
        [
            new DecisionRead<object>("selected", null, "store", "namespace", 24, [new EventType("created", 1)], new("source", "type", "selected", null)),
            new DecisionRead<object>("source", null, "store", "namespace", 24, [new EventType("created", 1)], new("unrelated", "type", "source", null)),
            DecisionRead<object>.Unprotected("source", null)
        ];
        _conflicts = result.GetDecisionConflicts(reads);
    }

    [Fact] void should_match_the_guarded_source_and_preserve_the_model_key() => _conflicts.ShouldContainOnly([new DecisionConflict(typeof(object), "selected"), new DecisionConflict(typeof(object), "source")]);
}
