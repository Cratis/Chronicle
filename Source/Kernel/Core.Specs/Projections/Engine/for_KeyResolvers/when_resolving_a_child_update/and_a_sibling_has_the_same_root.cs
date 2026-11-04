// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers.when_resolving_a_child_update;

public class and_a_sibling_has_the_same_root : given.an_assignment_created_by_a_root_mapping
{
    const string SiblingKey = "sibling-assignment";

    AppendedEvent _siblingUpdate;
    Key _assignmentKey;
    Key _siblingKey;

    void Establish()
    {
        WithCreationEvent(CreateEvent(2, AssignedEventType, SiblingKey));
        _siblingUpdate = CreateEvent(5, WithheldEventType, SiblingKey);
    }

    async Task Because()
    {
        _assignmentKey = ((ResolvedKey)await Resolver(Storage, Sink, WithheldEvent)).Key;
        _siblingKey = ((ResolvedKey)await Resolver(Storage, Sink, _siblingUpdate)).Key;
    }

    [Fact] void should_keep_the_original_assignment_location() => _assignmentKey.ArrayIndexers.GetFor("assignments").Identifier.ToString().ShouldEqual(AssignmentKey);
    [Fact] void should_target_only_the_sibling_for_its_update() => _siblingKey.ArrayIndexers.GetFor("assignments").Identifier.ToString().ShouldEqual(SiblingKey);
    [Fact] void should_resolve_the_sibling_under_the_same_root() => _siblingKey.Value.ShouldEqual(_assignmentKey.Value);
    [Fact] void should_not_append_the_sibling_to_the_original_indexers() => _assignmentKey.ArrayIndexers.Count.ShouldEqual(1);
}
