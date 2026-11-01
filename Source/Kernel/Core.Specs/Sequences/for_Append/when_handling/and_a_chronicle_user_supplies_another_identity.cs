// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Sequences.for_Append.when_handling;

public class and_a_chronicle_user_supplies_another_identity : given.an_append_endpoint
{
    void Establish() => _principal.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity(
        [new Claim("sub", "caller"), new Claim(ChronicleClaims.PrincipalKind, ChronicleClaims.User)], "Chronicle")));

    async Task Because() => await new Append("store", "namespace", "event-log", "source", "source-type", "stream-type", "stream", new("event", 1, false), "{}", CausedBy: new("other", "Other", "other", new("nested", "Nested", "nested", null)))
        .Handle(_grainFactory, _causation, _principal);

    [Fact]
    void should_record_the_caller() => ((Concepts.Identities.Identity)_eventSequence.ReceivedCalls()
        .Single(call => call.GetMethodInfo().Name == nameof(IEventSequence.Append)).GetArguments()[8]).Subject.ShouldEqual("caller");
}
