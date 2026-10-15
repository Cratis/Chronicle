// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_TriggerRegistrar;

public class when_registration_loses_a_revision_race : given.a_registrar
{
    ITriggerDefinitionsStorage _racing;

    void Establish()
    {
        _racing = Substitute.For<ITriggerDefinitionsStorage>();
        _racing.Get(Arg.Any<TriggerId>()).Returns(Task.FromResult<TriggerDefinition?>(null), Task.FromResult<TriggerDefinition?>(new(new TriggerId("payment-import"), "Another worker", "{}", 1)));
        _racing.TrySave(Arg.Any<TriggerDefinition>(), Arg.Any<long?>()).Returns(false, true);
    }

    async Task Because() => _result = await _registrar.Register(_racing, _command.Triggers);

    [Fact] void should_register_against_the_winning_revision() => _result.Registered.ShouldBeTrue();
    [Fact] async Task should_compare_against_the_durable_winner() => await _racing.Received(1).TrySave(Arg.Is<TriggerDefinition>(definition => definition.Revision == 2), 1);
}
