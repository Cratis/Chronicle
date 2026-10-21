// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_TriggerRegistrar;

public class when_registering_a_contract : given.a_registrar
{
    async Task Because()
    {
        _result = await _command.Handle(_storage, _registrar);
        _stored = await _definitions.Get(new TriggerId("payment-import"));
    }

    [Fact] void should_register() => _result.Registered.ShouldBeTrue();
    [Fact] void should_have_no_errors() => _result.Errors.ShouldBeEmpty();
    [Fact] void should_store_the_first_revision() => _stored!.Revision.ShouldEqual(1L);
    [Fact] void should_store_the_payload_contract() => _stored!.Schema.ShouldContain("integer");
}
