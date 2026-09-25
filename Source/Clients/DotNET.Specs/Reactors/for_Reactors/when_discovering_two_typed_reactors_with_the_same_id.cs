// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_Reactors;

public class when_discovering_two_typed_reactors_with_the_same_id : given.all_dependencies
{
    Exception _exception;

    void Establish() => _clientArtifactsProvider.Reactors.Returns([typeof(FirstReactor), typeof(SecondReactor)]);

    async Task Because() => _exception = await Catch.Exception(_reactors.Discover);

    [Fact] void should_reject_the_ambiguous_observer_id() => _exception.ShouldBeOfExactType<ReactorAlreadyRegistered>();

    [Reactor("shared-id")]
    class FirstReactor : IReactor;

    [Reactor("shared-id")]
    class SecondReactor : IReactor;
}
