// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_Reactors;

public class when_registering_without_event_types : given.all_dependencies
{
    Exception _exception;

    async Task Because() => _exception = await Catch.Exception(() => _reactors.Register("empty", _ => { }, (_, _) => Task.CompletedTask));

    [Fact] void should_reject_the_subscription() => _exception.ShouldBeOfExactType<NoEventTypesForReactor>();
}
