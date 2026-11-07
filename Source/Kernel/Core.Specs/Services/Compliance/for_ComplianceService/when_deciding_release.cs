// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Security;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Compliance.for_ComplianceService;

public class when_deciding_release : given.a_compliance_boundary
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task should_deny_before_schema_or_decryption_work_for_denial_or_lookup_failure(bool strict, bool throws)
    {
        _decisions.Decide(Arg.Any<ClaimsPrincipal?>(), WellKnownCapabilities.ReleaseCompliance, Arg.Any<AuthorizationScope>())
            .Returns(call => throws
                ? Task.FromException<AuthorizationDecision>(new InvalidOperationException("lookup detail"))
                : Task.FromResult(AuthorizationDecision.Deny(AuthorizationReason.NotGranted, call.Arg<Capability>(), call.Arg<AuthorizationScope>())));
        var exception = await Catch.Exception(() => _service.Release(ReleaseRequest(strict), _context));
        exception.ShouldBeOfExactType<RpcException>();
        ((RpcException)exception).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
        _schema.ReceivedCalls().ShouldBeEmpty();
        _grainFactory.ReceivedCalls().ShouldBeEmpty();
        _audit.Entries.ShouldEqual(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_deny_an_unidentified_http_caller_in_enforce_mode(bool strict)
    {
        UseCaller(new ClaimsPrincipal(new ClaimsIdentity()));
        var exception = await Catch.Exception(() => _service.Release(ReleaseRequest(strict), _context));
        exception.ShouldBeOfExactType<RpcException>();
        ((RpcException)exception).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
        _decisions.ReceivedCalls().ShouldBeEmpty();
        _schema.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task should_use_the_requested_store_and_namespace_for_release()
    {
        await _service.Release(ReleaseRequest(strict: false), _context);
        await _decisions.Received(1).Decide(
            Arg.Any<ClaimsPrincipal>(),
            WellKnownCapabilities.ReleaseCompliance,
            AuthorizationScope.ForNamespace(EventStore, EventStoreNamespace));
        _audit.Entries.ShouldEqual(1);
    }

    [Fact]
    public async Task should_continue_release_in_audit_mode_after_a_denial()
    {
        Configure(enforce: false);
        _decisions.Decide(Arg.Any<ClaimsPrincipal?>(), WellKnownCapabilities.ReleaseCompliance, Arg.Any<AuthorizationScope>())
            .Returns(call => AuthorizationDecision.Deny(AuthorizationReason.NotGranted, call.Arg<Capability>(), call.Arg<AuthorizationScope>()));
        await _service.Release(ReleaseRequest(strict: false), _context);
        _schema.ReceivedCalls().ShouldNotBeEmpty();
        _audit.Entries.ShouldEqual(1);
    }
}
