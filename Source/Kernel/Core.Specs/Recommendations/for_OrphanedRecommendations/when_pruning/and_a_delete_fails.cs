// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Recommendations.for_OrphanedRecommendations.when_pruning;

public class and_a_delete_fails : given.a_pruner
{
    Exception _error;

    void Establish()
    {
        _rows.Add(Replay("failed"));
        _recommendations.Remove(_rows[0].Id).Returns(Task.FromException(new IOException("Simulated delete failure")));
    }

    async Task Because() => _error = await Catch.Exception(Prune);

    [Fact] void should_report_the_failure_to_the_caller() => _error.ShouldBeOfExactType<IOException>();
}
