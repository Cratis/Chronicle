// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Queries;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Represents an implementation of <see cref="IFederationPolicies"/>.
/// </summary>
/// <param name="connection">The <see cref="IChronicleConnection"/> to the kernel to act on.</param>
public class FederationPolicies(IChronicleConnection connection) : IFederationPolicies
{
    readonly IChronicleServicesAccessor _servicesAccessor = (connection as IChronicleServicesAccessor)!;

    /// <inheritdoc/>
    public async Task Set(string subject, FederationPolicyEntry entry, CancellationToken cancellationToken = default)
    {
        var service = await GetService(cancellationToken);
        await service.SetFederationPolicy(
            new()
            {
                Subject = subject,
                Direction = ToContract(entry.Direction),
                EventStore = entry.EventStore,
                Namespace = entry.Namespace,
                AllPublic = entry.AllPublic,
                EventTypes = [.. entry.EventTypes]
            },
            CallContextFor(cancellationToken)).EnsureSuccess();
    }

    /// <inheritdoc/>
    public async Task Suspend(string subject, CancellationToken cancellationToken = default)
    {
        var service = await GetService(cancellationToken);
        await service.SuspendFederationPolicy(new() { Subject = subject }, CallContextFor(cancellationToken)).EnsureSuccess();
    }

    /// <inheritdoc/>
    public async Task Resume(string subject, CancellationToken cancellationToken = default)
    {
        var service = await GetService(cancellationToken);
        await service.ResumeFederationPolicy(new() { Subject = subject }, CallContextFor(cancellationToken)).EnsureSuccess();
    }

    /// <inheritdoc/>
    public async Task Revoke(string subject, CancellationToken cancellationToken = default)
    {
        var service = await GetService(cancellationToken);
        await service.RevokeFederationPolicy(new() { Subject = subject }, CallContextFor(cancellationToken)).EnsureSuccess();
    }

    /// <inheritdoc/>
    public async Task RevokeDirection(string subject, FederationDirection direction, CancellationToken cancellationToken = default)
    {
        var service = await GetService(cancellationToken);
        await service.RevokeFederationPolicyDirection(
            new() { Subject = subject, Direction = ToContract(direction) },
            CallContextFor(cancellationToken)).EnsureSuccess();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<FederationPolicyInformation>> GetPolicies(CancellationToken cancellationToken = default)
    {
        var service = await GetService(cancellationToken);
        var policies = await service.AllPolicies(CallContextFor(cancellationToken)).EnsureSuccess();
        return
        [
            .. policies.Select(policy => new FederationPolicyInformation(
                policy.Subject,
                (FederationPolicyStatus)(int)policy.Status,
                [.. policy.Entries.Select(entry => new FederationPolicyEntry(
                    (FederationDirection)(int)entry.Direction,
                    entry.EventStore,
                    entry.Namespace,
                    entry.AllPublic,
                    [.. entry.EventTypes]))],
                policy.ChangedBy))
        ];
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<FederationLinkInformation>> GetLinks(CancellationToken cancellationToken = default)
    {
        var service = await GetService(cancellationToken);
        var links = await service.AllSubjectLinks(CallContextFor(cancellationToken)).EnsureSuccess();
        return
        [
            .. links.Select(link =>
            {
                DateTimeOffset? lastPull = link.OutboundLastPull;
                DateTimeOffset? lastReceived = link.InboundLastReceived;
                DateTimeOffset? lastActivity = link.LastActivity;
                ulong? inboundLag = string.IsNullOrEmpty(link.InboundState) ? null : link.InboundLag;
                return new FederationLinkInformation(
                    link.Subject,
                    link.Connected,
                    lastActivity,
                    new(lastPull, link.OutboundEvents, link.OutboundBytes, null),
                    new(lastReceived, link.InboundEvents, link.InboundBytes, inboundLag),
                    link.OutboundThrottled,
                    link.InboundState,
                    link.LastError);
            })
        ];
    }

    /// <inheritdoc/>
    public async Task<FederationLinkInformation?> GetLink(string subject, CancellationToken cancellationToken = default) =>
        (await GetLinks(cancellationToken)).FirstOrDefault(_ => string.Equals(_.Subject, subject, StringComparison.Ordinal));

    static CallContext CallContextFor(CancellationToken cancellationToken) => new(new CallOptions(cancellationToken: cancellationToken));

    static Contracts.Federation.FederationDirection ToContract(FederationDirection direction) => (Contracts.Federation.FederationDirection)(int)direction;

    async Task<Contracts.Federation.IFederation> GetService(CancellationToken cancellationToken) =>
        (await _servicesAccessor.GetServices(cancellationToken)).Federation;
}
