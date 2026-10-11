// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_discovering_filtered_events;

public class with_an_unregistered_public_handler : given.a_filtered_provider
{
    Exception _error;
    protected override bool IncludeMissingHandler => true;

    async Task Because() => _error = await Catch.Exception(_reactors.Discover);

    [Fact] void should_reject_the_missing_dispatch_event() => _error.ShouldBeOfExactType<TypeIsNotAnEventType>();
    [Fact] void should_not_publish_a_partial_discovery() => _reactors.Handlers.ShouldBeEmpty();
}
