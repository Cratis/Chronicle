// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.EventStores.for_EventStoreDeploymentRegistry.when_registering;

public class and_the_append_fails : given.a_registry
{
    Exception _failure;
    protected override DeploymentState Stored => new(10UL, "previous");

    void Establish() => _systemSequence.Append(Arg.Any<EventSourceId>(), Arg.Any<object>(), default, default, default, default, default, default, default)
        .Returns(new AppendResult { Errors = ["Append failed"] });

    async Task Because() => _failure = await Catch.Exception(() => _registry.Register(11UL, "latest"));

    [Fact] void should_fail_loudly() => _failure.ShouldBeOfExactType<EventStoreDeploymentRegistrationFailed>();
    [Fact] async Task should_keep_the_stored_state() => (await _registry.Get()).ShouldEqual(Stored);
}
