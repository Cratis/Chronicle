// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers.when_resolving_a_child_update;

public class and_creation_also_maps_a_differently_keyed_root : given.an_assignment_created_by_a_root_mapping
{
    Key _withheldKey;
    Key _endedKey;
    Key _replayedKey;

    async Task Because()
    {
        _withheldKey = ((ResolvedKey)await Resolver(Storage, Sink, WithheldEvent)).Key;
        _endedKey = ((ResolvedKey)await Resolver(Storage, Sink, EndedEvent)).Key;
        _replayedKey = ((ResolvedKey)await Resolver(Storage, Sink, WithheldEvent)).Key;
    }

    [Fact] void should_resolve_withheld_update_to_the_root() => _withheldKey.Value.ShouldEqual(ProjectAssetKey);
    [Fact] void should_resolve_ended_update_to_the_root() => _endedKey.Value.ShouldEqual(ProjectAssetKey);
    [Fact] void should_include_one_indexer_for_withheld_update() => _withheldKey.ArrayIndexers.Count.ShouldEqual(1);
    [Fact] void should_include_one_indexer_for_ended_update() => _endedKey.ArrayIndexers.Count.ShouldEqual(1);
    [Fact] void should_target_the_withheld_assignment() => _withheldKey.ArrayIndexers.GetFor("assignments").Identifier.ToString().ShouldEqual(AssignmentKey);
    [Fact] void should_target_the_ended_assignment() => _endedKey.ArrayIndexers.GetFor("assignments").Identifier.ToString().ShouldEqual(AssignmentKey);
    [Fact] void should_identify_assignments_by_their_identity() => _withheldKey.ArrayIndexers.GetFor("assignments").IdentifierProperty.ShouldEqual((PropertyPath)"assignmentId");
    [Fact] void should_resolve_the_same_root_on_replay() => _replayedKey.Value.ShouldEqual(_withheldKey.Value);
    [Fact] void should_resolve_the_same_child_location_on_replay() => _replayedKey.ArrayIndexers.ShouldEqual(_withheldKey.ArrayIndexers);
}
