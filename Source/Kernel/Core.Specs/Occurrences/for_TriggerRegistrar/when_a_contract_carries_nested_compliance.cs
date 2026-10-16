// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences.for_TriggerRegistrar;

public class when_a_contract_carries_nested_compliance : given.a_registrar
{
    void Establish() => _command = _command with
    {
        Triggers = _command.Triggers.Concat([new TriggerRegistration("private", "Private data", "{\"type\":\"object\",\"properties\":{\"nested\":{\"type\":\"array\",\"items\":{\"type\":\"string\",\"compliance\":[\"pii\"]}}}}")])
    };

    async Task Because() => _result = await _command.Handle(_storage, _registrar);

    [Fact] void should_refuse_the_registration() => _result.Registered.ShouldBeFalse();
    [Fact] void should_name_the_compliance_refusal() => _result.Errors.ShouldContain("TriggerSchemaCarriesCompliance:private");
    [Fact] async Task should_not_write_even_the_valid_contract() => (await _definitions.GetAll()).ShouldBeEmpty();
}
