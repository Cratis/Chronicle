// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;

namespace Cratis.Chronicle.Reactors.for_ReactorHandler;

public class when_handling_on_next_with_duplicate_identity_in_chain : given.a_reactor_handler
{
    EventContext _eventContext;
    Identity _identityPassedToProvider;

    void Establish()
    {
        _eventContext = EventContext.Empty with
        {
            EventType = new(Guid.NewGuid().ToString(), 1),
            SequenceNumber = 42,
            CausedBy = Identity.System with { OnBehalfOf = new Identity("user-subject", "User", "user") }
        };

        _identityProvider
            .When(_ => _.SetCurrentIdentity(Arg.Any<Identity>()))
            .Do(call => _identityPassedToProvider = call.Arg<Identity>());
    }

    async Task Because() => await handler.OnNext(_eventContext, new SomeEvent("test"), _reactorInvoker);

    [Fact] void should_set_identity_without_duplicates() => _identityPassedToProvider.Subject.ShouldEqual(Identity.System.Subject);
    [Fact] void should_not_have_duplicate_system_on_behalf_of() => _identityPassedToProvider.OnBehalfOf!.Subject.ShouldEqual("user-subject");
    [Fact] void should_have_no_further_chain() => _identityPassedToProvider.OnBehalfOf!.OnBehalfOf.ShouldBeNull();
}
