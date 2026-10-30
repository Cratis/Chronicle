// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationInitializer;

public class when_the_flag_is_off : given.an_initializer
{
    void Establish() => _flag.Enabled.Returns(false);

    async Task Because() => _result = await _initializer.Initialize(_name);
    [Fact] void should_return_disabled() => _result.ShouldBeOfExactType<GenerationInitializationDisabled>();
    [Fact] void should_not_access_storage() => _storage.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_access_the_store() => _store.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_check_proof() => _proof.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_check_membership() => _membership.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_read_legacy_work() => _legacy.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_read_materializations() => _models.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_write() => _writes.ShouldEqual(0);
}
