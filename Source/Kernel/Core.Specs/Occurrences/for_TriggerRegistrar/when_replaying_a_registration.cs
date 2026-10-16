// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_TriggerRegistrar;

public class when_replaying_a_registration : given.a_registrar
{
    async Task Establish() => await _command.Handle(_storage, _registrar);

    async Task Because()
    {
        _result = await _command.Handle(_storage, _registrar);
        _stored = await _definitions.Get(new TriggerId("payment-import"));
    }

    [Fact] void should_succeed() => _result.Registered.ShouldBeTrue();
    [Fact] void should_preserve_the_revision() => _stored!.Revision.ShouldEqual(1L);
}
