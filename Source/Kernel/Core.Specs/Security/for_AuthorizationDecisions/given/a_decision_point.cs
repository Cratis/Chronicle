// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security.for_AuthorizationDecisions.given;

public class a_decision_point : Specification
{
    protected const string Secret = "[REDACTED:connection-string]";
    protected IAdministratorAuthorization _administrators;
    protected ClaimsPrincipal _principal;
    protected RecordingLogger _logger;
    protected bool _authenticationEnabled;
    protected string[] _enrollingApplications = [];
    protected bool _internalAuthority = true;
    protected AuthorizationDecision _decision;

    public static TheoryData<string> EnrolledCapabilities() => new()
    {
        WellKnownCapabilities.AdministerSecurity.Value,
        WellKnownCapabilities.OperateEventStore.Value,
        WellKnownCapabilities.AccessSystemStore.Value,
        WellKnownCapabilities.EnrollFederation.Value
    };

    protected AuthorizationDecisions CreateDecisions() => new(
        Options.Create(new ChronicleOptions
        {
            Authentication = new Authentication { Enabled = _authenticationEnabled, Authority = _internalAuthority ? null : "https://idp.example" },
            Federation = new Configuration.Federation { Enrollment = new FederationEnrollmentOptions { Applications = _enrollingApplications } }
        }),
        _logger,
        httpContextAccessor: Security.given.caller_services.For(_administrators));

    protected Task<AuthorizationDecision> Decide(ClaimsPrincipal? principal, Capability capability, AuthorizationScope scope) =>
        CreateDecisions().Decide(principal, capability, scope);

    protected static void ShouldHaveDecided(AuthorizationDecision decision, AuthorizationOutcome outcome, AuthorizationReason reason)
    {
        decision.Outcome.ShouldEqual(outcome);
        decision.Reason.ShouldEqual(reason);
    }

    protected void ShouldHaveDecided(AuthorizationOutcome outcome, AuthorizationReason reason) => ShouldHaveDecided(_decision, outcome, reason);

    protected static ClaimsPrincipal ApplicationToken(string clientId) => new(new ClaimsIdentity(
        [new Claim("sub", Guid.NewGuid().ToString()), new Claim("name", clientId)], "Bearer"));

    protected void LookupShouldNotHaveBeenCalled() => _administrators.DidNotReceiveWithAnyArgs().IsAuthorized(default!);

    void Establish()
    {
        _authenticationEnabled = true;
        _administrators = Substitute.For<IAdministratorAuthorization>();
        _principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("access_token", Secret), new Claim("client_secret", Secret), new Claim(ChronicleClaimTypes.Principal, ChronicleClaimTypes.User)], "Bearer"));
        _logger = new RecordingLogger();
    }

    public class RecordingLogger : ILogger<AuthorizationDecisions>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception) + exception?.ToString()));
    }
}
