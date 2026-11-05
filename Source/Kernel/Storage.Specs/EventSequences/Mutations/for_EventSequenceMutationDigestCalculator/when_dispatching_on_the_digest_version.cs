// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationDigestCalculator;

public class when_dispatching_on_the_digest_version : given.a_digest_calculation
{
    const EventSequenceMutationDigestVersion SecondVersion = (EventSequenceMutationDigestVersion)2;

    EventSequenceMutationDefinitionDigestV1 _first;
    EventSequenceMutationDefinitionDigestV1 _secondDigest;
    EventSequenceMutationDefinitionDigestV1 _firstAfterSwitch;
    EventSequenceMutationDefinition _definition;
    EventSequenceMutationValidationResult _valid;
    EventSequenceMutationValidationResult _unsupported;
    Exception _thrown;

    void Because()
    {
        var current = EventSequenceMutationDigestCalculator.CurrentDigestVersion;
        _first = EventSequenceMutationDigestCalculator.CalculateDefinitionDigest(_scope, _mutation.Definition.Request, _mutation.Target, current);
        using (EventSequenceMutationDigestSeam.RegisterForTest(SecondVersion, (_, bytes) => SHA256.HashData([2, .. bytes]), makeCurrent: true))
        {
            _secondDigest = EventSequenceMutationDigestCalculator.CalculateDefinitionDigest(_scope, _mutation.Definition.Request, _mutation.Target, SecondVersion);
            _firstAfterSwitch = EventSequenceMutationDigestCalculator.CalculateDefinitionDigest(_scope, _mutation.Definition.Request, _mutation.Target, current);
            _definition = EventSequenceMutationDefinition.Create(_scope, _mutation.Definition.Request, _mutation.Target, current);
            _valid = EventSequenceMutationValidator.ValidateDefinition(_scope, _definition);
        }

        _unsupported = EventSequenceMutationValidator.ValidateDefinition(_scope, _definition with { DefinitionDigestVersion = SecondVersion });
        _thrown = Catch.Exception(() => EventSequenceMutationDigestCalculator.CalculateDefinitionDigest(_scope, _mutation.Definition.Request, _mutation.Target, SecondVersion))!;
    }

    [Fact] void should_compute_a_different_digest_for_a_different_version() => _secondDigest.ShouldNotEqual(_first);
    [Fact] void should_keep_computing_the_persisted_version_after_the_write_version_changes() => _firstAfterSwitch.ShouldEqual(_first);
    [Fact] void should_keep_the_persisted_version_on_the_definition() => _definition.DefinitionDigestVersion.ShouldEqual(EventSequenceMutationDigestVersion.Sha256DomainSeparated);
    [Fact] void should_validate_a_definition_under_its_persisted_version() => _valid.IsValid.ShouldBeTrue();
    [Fact] void should_reject_an_unsupported_definition_version() => _unsupported.IsValid.ShouldBeFalse();
    [Fact] void should_throw_for_an_unsupported_version() => _thrown.ShouldBeOfExactType<UnsupportedEventSequenceMutationDigestVersion>();
}
