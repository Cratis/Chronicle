// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Security;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Sets what a hosted federation subject may do in one direction, creating the policy of the subject when it has none.
/// </summary>
/// <remarks>
/// The target is decided here, by the operator, and never by the certificate or the request. The ceiling is the set of
/// public event types. A revoked policy cannot be set again; use a new subject.
/// </remarks>
/// <param name="Subject">The opaque subject the policy is for, the customer part of the peer's identity.</param>
/// <param name="Direction">The direction the entry governs, seen from this kernel.</param>
/// <param name="EventStore">The local event store the subject's link addresses.</param>
/// <param name="Namespace">The namespace of that event store the subject's link addresses.</param>
/// <param name="AllPublic">Whether the entry covers every public event type.</param>
/// <param name="EventTypes">The specific event types allowed when <paramref name="AllPublic"/> is false.</param>
[Command]
[Cratis.Arc.Authorization.Authorize(Policy = WellKnownAuthorizationPolicies.AdministratorOperations)]
[BelongsTo(WellKnownServices.Federation)]
public record SetFederationPolicy(string Subject, FederationDirection Direction, string EventStore, string Namespace, bool AllPublic, IEnumerable<string> EventTypes)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="policies">The <see cref="IFederationPolicies"/> to set the policy with.</param>
    /// <param name="principalAccessor">The authenticated caller.</param>
    /// <param name="options">The server configuration.</param>
    /// <returns>Awaitable task.</returns>
    public Task Handle(IFederationPolicies policies, ICurrentPrincipalAccessor principalAccessor, IOptions<ChronicleOptions> options) =>
        policies.Set(Subject, Direction, EventStore, Namespace, AllPublic, EventTypes, PrincipalKey.Actor(principalAccessor.Current, options.Value.Authentication));
}
