// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;

namespace Cratis.Chronicle.Events.Constraints.Operator.for_ReindexConstraints;

public class when_all_constraints_are_requested : given.a_reindex_command
{
    async Task Because() => _result = await _scenario.Execute(new("store", "ns", "log", []));

    [Fact] void should_accept_the_command() => _result.ShouldBeSuccessful();
    [Fact] void should_name_unique_and_closing_definitions() => _request.Changes.Select(change => change.Name.Value).ShouldEqual(["unique", "closing"]);
    [Fact] void should_rebuild_the_requested_sequence() => _request.EventSequenceId.Value.ShouldEqual("log");
    [Fact] void should_refresh_before_starting() => Received.InOrder(() =>
    {
        _sequence.RefreshConstraints();
        _jobs.Start<IReindexConstraints, ReindexConstraintsRequest>(Arg.Any<ReindexConstraintsRequest>());
    });
}
