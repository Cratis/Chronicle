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
    protected AuthorizationDecision _decision;

    public static TheoryData<string> EnrolledCapabilities() => new()
    {
        WellKnownCapabilities.AdministerSecurity.Value,
        WellKnownCapabilities.OperateEventStore.Value,
        WellKnownCapabilities.AccessSystemStore.Value
    };

    protected AuthorizationDecisions CreateDecisions() => new(
        _administrators,
        Options.Create(new ChronicleOptions { Authentication = new Authentication { Enabled = _authenticationEnabled } }),
        _logger);

    protected Task<AuthorizationDecision> Decide(ClaimsPrincipal? principal, Capability capability, AuthorizationScope scope) =>
        CreateDecisions().Decide(principal, capability, scope);

    protected void ShouldHaveDecided(AuthorizationOutcome outcome, AuthorizationReason reason)
    {
        _decision.Outcome.ShouldEqual(outcome);
        _decision.Reason.ShouldEqual(reason);
    }

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
