// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;

namespace Cratis.Chronicle.Occurrences.for_RetryQuarantinedOccurrence.when_handling;

public class with_an_authenticated_principal : Specification
{
    readonly IOccurrenceQueue _queue = Substitute.For<IOccurrenceQueue>();
    readonly IGrainFactory _factory = Substitute.For<IGrainFactory>();
    readonly ICurrentPrincipalAccessor _principal = Substitute.For<ICurrentPrincipalAccessor>();

    void Establish()
    {
        _factory.GetGrain<IOccurrenceQueue>(Arg.Any<string>()).Returns(_queue);
        _principal.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "authenticated-id"), new Claim(ClaimTypes.Name, "Display name")], "test")));
    }

    async Task Because() => await new RetryQuarantinedOccurrence(new("store"), "tenant", "trigger", "occurrence").Handle(_factory, _principal);
    [Fact] async Task should_pass_the_authenticated_identity_to_the_atomic_rearm() => await _queue.Received(1).Retry("occurrence", "authenticated-id");
    [Fact] void should_require_explicit_operator_authorization() => typeof(RetryQuarantinedOccurrence).IsDefined(typeof(AuthorizeAttribute), true).ShouldBeTrue();
}
