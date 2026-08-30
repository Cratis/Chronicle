// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Patterns;
using Pattern = Cratis.Chronicle.Contracts.Patterns.Pattern;

namespace Cratis.Chronicle.Services.Patterns.for_Patterns.when_getting_usual_actions;

/// <summary>
/// The scope selects whose behavior is being asked about. Without one there is no question to answer, and the read
/// this query does is per scope - so it must be turned away before it reads rather than after.
/// </summary>
public class without_a_scope : given.a_patterns_service
{
    IEnumerable<Pattern> _result;

    async Task Because() => _result = await _service.GetUsualActions(new()
    {
        EventStore = EventStore,
        Namespace = EventStoreNamespaceName.Default,
        GroupingKey = PatternGroupingKey.Unspecified,
        Context = new Dictionary<string, string> { { FacetName.Day.Value, "Monday" } }
    });

    [Fact] void should_return_nothing() => _result.ShouldBeEmpty();

    [Fact] async Task should_not_read_anything() =>
        await _patterns.DidNotReceive().GetForScope(Arg.Any<PatternGroupingKey>());
}
