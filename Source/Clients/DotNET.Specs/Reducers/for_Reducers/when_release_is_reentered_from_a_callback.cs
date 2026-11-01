// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_Reducers;

public class when_release_is_reentered_from_a_callback : given.two_discovered_reducers
{
    CancellationTokenRegistration _registration;

    void Establish() => _registration = _firstToken.Register(_reducers.Release);

    void Because() => _reducers.Release();

    void Destroy() => _registration.Dispose();

    [Fact] void should_finish_cancelling_the_original_handlers() => (_firstToken.IsCancellationRequested && _secondToken.IsCancellationRequested).ShouldBeTrue();
    [Fact] void should_empty_the_indexes() => _reducers.GetAllHandlers().ShouldBeEmpty();
    [Fact] void should_dispose_the_first_handler() => Catch.Exception(() => _ = _first.CancellationToken).ShouldBeOfExactType<ObjectDisposedException>();
    [Fact] void should_dispose_the_second_handler() => Catch.Exception(() => _ = _second.CancellationToken).ShouldBeOfExactType<ObjectDisposedException>();
}
