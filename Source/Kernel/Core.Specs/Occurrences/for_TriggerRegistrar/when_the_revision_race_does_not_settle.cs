// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_TriggerRegistrar;

public class when_the_revision_race_does_not_settle : given.a_registrar
{
    ITriggerDefinitionsStorage _racing;

    void Establish() => _racing = Substitute.For<ITriggerDefinitionsStorage>();
    async Task Because() => _result = await _registrar.Register(_racing, _command.Triggers);

    [Fact] void should_not_claim_success() => _result.Registered.ShouldBeFalse();
    [Fact] void should_name_the_unsettled_registration() => _result.Errors.ShouldContain("TriggerRegistrationConflicted:payment-import");
    [Fact] async Task should_bound_the_compare_and_set_attempts() => await _racing.Received(8).TrySave(Arg.Any<TriggerDefinition>(), null);
}
