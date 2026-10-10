// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Monads;

namespace Cratis.Chronicle.Sequences.for_ReopenStreamScope.when_handling;

public class and_a_different_actor_is_supplied : Sequences.given.an_append_endpoint
{
    Concepts.Identities.Identity _actor;
    ReopenStreamScopeOutcome _result;

    void Establish()
    {
        _principal.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "operator")], "test")));
        _eventSequence.ReopenCompletedStream(Arg.Any<ClosedStreamScope>(), Arg.Any<string>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(), Arg.Any<Concepts.Identities.Identity>())
            .Returns(call =>
            {
                _actor = call.ArgAt<Concepts.Identities.Identity>(4);
                return Result<ReopenStreamScopeError>.Success();
            });
    }

    async Task Because() => _result = await new ReopenStreamScope("store", "namespace", "event-log", "Repair", EventSourceId: "source", CausedBy: new("forged", "Forged", "forged", null)).Handle(_grainFactory, _causation, _principal);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_audit_the_authenticated_operator_instead() => _actor.Subject.ShouldEqual("operator");
}
