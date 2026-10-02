// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverRemover.when_removing;

public class and_the_observer_grain_is_still_activated : given.all_dependencies
{
    readonly List<string> _sequence = [];

    void Establish()
    {
        _observerInFirstNamespace.When(observer => observer.Remove()).Do(_ => _sequence.Add("first"));
        _observerInSecondNamespace.When(observer => observer.Remove()).Do(_ => _sequence.Add("second"));
        _observerDefinitions.When(definitions => definitions.Delete(_observerId)).Do(_ => _sequence.Add("definition"));
    }

    async Task Because() => await Remove();

    [Fact] void should_remove_each_namespace_before_the_shared_definition() => _sequence.ShouldEqual(["first", "second", "definition"]);
}
