// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.Operator.for_ReindexConstraints;

public class when_a_subset_is_requested : given.a_reindex_command
{
    async Task Because() => _result = await _scenario.Execute(new("store", "ns", "log", ["closing"]));

    [Fact] void should_accept_the_command() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_rebuild_only_the_named_definition() => _request.Changes.Single().Name.Value.ShouldEqual("closing");
}
