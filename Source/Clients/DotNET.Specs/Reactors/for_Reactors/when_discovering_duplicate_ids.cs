// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_Reactors;

public class when_discovering_duplicate_ids : given.a_registered_delegate
{
    Exception _exception;
    CancellationToken _originalToken;

    void Establish()
    {
        _originalToken = _handler.CancellationToken;
        _clientArtifactsProvider.Reactors.Returns([typeof(CollidingReactor)]);
    }

    async Task Because() => _exception = await Catch.Exception(_reactors.Discover);

    [Fact] void should_name_the_duplicate_registration() => _exception.ShouldBeOfExactType<ReactorAlreadyRegistered>();
    [Fact] void should_leave_the_existing_handler_connected() => _originalToken.IsCancellationRequested.ShouldBeFalse();
    [Fact] void should_leave_the_existing_handler_available() => _reactors.GetHandlerById("bridge").ShouldEqual(_handler);

    [Reactor("bridge")]
    class CollidingReactor : IReactor;
}
