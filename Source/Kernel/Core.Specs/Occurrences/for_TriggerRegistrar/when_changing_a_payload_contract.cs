// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_TriggerRegistrar;

public class when_changing_a_payload_contract : given.a_registrar
{
    async Task Establish()
    {
        await _command.Handle(_storage, _registrar);
        _command = _command with { Triggers = [new("payment-import", "Payment importer", "{\"type\":\"object\",\"properties\":{\"count\":{\"type\":\"string\"}}}")] };
    }

    async Task Because()
    {
        _result = await _command.Handle(_storage, _registrar);
        _stored = await _definitions.Get(new TriggerId("payment-import"));
    }

    [Fact] void should_succeed() => _result.Registered.ShouldBeTrue();
    [Fact] void should_advance_the_revision() => _stored!.Revision.ShouldEqual(2L);
    [Fact] void should_replace_the_payload_contract() => _stored!.Schema.ShouldContain("string");
}
