// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.for_ProjectionRegistrationSignature;

public class when_the_registration_context_differs : a_registration
{
    string _original;
    string _anotherRegistrant;
    string _anotherVersion;
    string _anotherFullSetOwner;
    string _noFullSet;
    string _extraDefinition;

    void Because()
    {
        var definition = Definition();
        _original = ProjectionRegistrationSignature.For([definition], ProjectionOwner.Client, "the-app", "1.0.0");
        _anotherRegistrant = ProjectionRegistrationSignature.For([definition], ProjectionOwner.Client, "another-app", "1.0.0");
        _anotherVersion = ProjectionRegistrationSignature.For([definition], ProjectionOwner.Client, "the-app", "1.0.1");
        _anotherFullSetOwner = ProjectionRegistrationSignature.For([definition], ProjectionOwner.Kernel, "the-app", "1.0.0");
        _noFullSet = ProjectionRegistrationSignature.For([definition], null, "the-app", "1.0.0");
        _extraDefinition = ProjectionRegistrationSignature.For([definition, Definition("another")], ProjectionOwner.Client, "the-app", "1.0.0");
    }

    [Fact] void should_differ_for_another_registrant() => _anotherRegistrant.ShouldNotEqual(_original);
    [Fact] void should_differ_for_another_version() => _anotherVersion.ShouldNotEqual(_original);
    [Fact] void should_differ_for_another_full_set_owner() => _anotherFullSetOwner.ShouldNotEqual(_original);
    [Fact] void should_differ_when_it_is_not_a_full_set() => _noFullSet.ShouldNotEqual(_original);
    [Fact] void should_differ_when_a_definition_is_added() => _extraDefinition.ShouldNotEqual(_original);
}
