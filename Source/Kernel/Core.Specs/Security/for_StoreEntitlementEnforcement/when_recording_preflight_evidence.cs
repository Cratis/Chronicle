// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Security.for_StoreEntitlementEnforcement;

public class when_recording_preflight_evidence : Specification
{
    [Fact]
    public async Task should_record_only_one_rate_limited_observation_without_waiting_for_the_grain()
    {
        var grains = Substitute.For<IGrainFactory>();
        var observations = Substitute.For<IStoreEntitlementObservations>();
        grains.GetGrain<IStoreEntitlementObservations>(IStoreEntitlementObservations.Key).Returns(observations);
        var recorded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        observations.Record(Arg.Any<PrincipalKey>(), "orders", "tenant", StoreAccess.Read).Returns(_ =>
        {
            recorded.TrySetResult();
            return pending.Task;
        });
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("iss", "issuer"), new Claim("sub", "subject")], "spec"));
        var authentication = new Authentication();
        var loggerFactory = new NullLoggerFactory();
        var decision = AuthorizationDecision.Deny(AuthorizationReason.NotGranted, WellKnownCapabilities.ReadEventStore, AuthorizationScope.ForNamespace("orders", "tenant"));
        try
        {
            StoreEntitlementEnforcement.Audit(decision, principal, authentication, loggerFactory, grains);
            StoreEntitlementEnforcement.Audit(decision, principal, authentication, loggerFactory, grains);
            await recorded.Task.WaitAsync(TimeSpan.FromSeconds(10));
            pending.Task.IsCompleted.ShouldBeFalse();
            _ = observations.Received(1).Record(new(PrincipalKey.LocalIssuer, "subject"), "orders", "tenant", StoreAccess.Read);
        }
        finally
        {
            pending.TrySetResult();
        }
    }
}
