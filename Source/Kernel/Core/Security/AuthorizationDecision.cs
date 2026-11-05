// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// The result of deciding whether a principal holds a capability in a scope.
/// </summary>
/// <param name="Outcome">The outcome.</param>
/// <param name="Reason">The machine-readable reason.</param>
/// <param name="Capability">The capability that was asked for.</param>
/// <param name="Scope">The scope it was asked for in.</param>
public record AuthorizationDecision(AuthorizationOutcome Outcome, AuthorizationReason Reason, Capability Capability, AuthorizationScope Scope)
{
    /// <summary>
    /// Gets whether the decision allows the request.
    /// </summary>
    public bool IsAllowed => Outcome == AuthorizationOutcome.Allowed;

    /// <summary>
    /// Creates an allowing decision.
    /// </summary>
    /// <param name="reason">The reason.</param>
    /// <param name="capability">The capability.</param>
    /// <param name="scope">The scope.</param>
    /// <returns>The decision.</returns>
    public static AuthorizationDecision Allow(AuthorizationReason reason, Capability capability, AuthorizationScope scope) =>
        new(AuthorizationOutcome.Allowed, reason, capability, scope);

    /// <summary>
    /// Creates a denying decision.
    /// </summary>
    /// <param name="reason">The reason.</param>
    /// <param name="capability">The capability.</param>
    /// <param name="scope">The scope.</param>
    /// <returns>The decision.</returns>
    public static AuthorizationDecision Deny(AuthorizationReason reason, Capability capability, AuthorizationScope scope) =>
        new(AuthorizationOutcome.Denied, reason, capability, scope);
}
