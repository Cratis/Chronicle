// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;

namespace Cratis.Chronicle.Federation.for_FederationCausation;

public class when_round_tripping : Specification
{
    Causation _causation;
    FederationResumePoint _found;
    FederationResumePoint _foundAmongOthers;
    FederationResumePoint _foundInUnrelated;

    void Because()
    {
        _causation = FederationCausation.Create("remote", 4242, "source", DateTimeOffset.UnixEpoch);
        _found = FederationCausation.Read([_causation]);
        var other = new Causation(DateTimeOffset.UnixEpoch, "Other", new Dictionary<string, string>());
        _foundAmongOthers = FederationCausation.Read([other, _causation]);
        _foundInUnrelated = FederationCausation.Read([other]);
    }

    [Fact] void should_use_the_federation_type() => ((string)_causation.Type).ShouldEqual("Federation");
    [Fact] void should_read_back_the_origin() => _found.OriginEventStore.ShouldEqual("remote");
    [Fact] void should_read_back_the_source_sequence_number() => _found.LastSourceSequenceNumber.ShouldEqual(4242UL);
    [Fact] void should_find_it_among_other_causations() => _foundAmongOthers.ShouldEqual(_found);
    [Fact] void should_find_nothing_without_a_federation_causation() => _foundInUnrelated.ShouldBeNull();
}
