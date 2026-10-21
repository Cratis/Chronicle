// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ReadProtectionMerge.when_resolving;

public class with_changed_caller_type_collections : given.contributions
{
    void Establish()
    {
        var types = new List<EventType> { _stockReduced };
        Add(ReadProtectionContributionKind.ProtectedRead, new(100, _label, EventTypes: types));
        types.Clear();
        types.Add(_stockRemoved);
    }

    void Because() => _plan = Merge();

    [Fact] void should_retain_the_types_captured_at_acquisition() => _plan.Scopes.Single().Value.EventTypes!.Select(_ => _.Id).ShouldContainOnly(_stockReduced.Id);
}
