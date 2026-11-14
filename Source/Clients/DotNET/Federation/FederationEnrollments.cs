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
/// Represents an implementation of <see cref="IFederationEnrollments"/>.
/// </summary>
/// <param name="connection">The <see cref="IChronicleConnection"/> to the kernel to act on.</param>
public class FederationEnrollments(IChronicleConnection connection) : IFederationEnrollments
{
    readonly IChronicleServicesAccessor _servicesAccessor = (connection as IChronicleServicesAccessor)!;

    /// <inheritdoc/>
    public async Task<FederationEnrollmentCode> Create(string subject, string? instance = default, CancellationToken cancellationToken = default)
    {
        var service = await GetService(cancellationToken);
        var response = await service.CreateFederationEnrollment(
            new()
            {
                Subject = subject,
                Instance = instance ?? string.Empty
            },
            CallContextFor(cancellationToken)).EnsureSuccess();
        return new(response.Id, response.Code, response.ExpiresAt);
    }

    /// <inheritdoc/>
    public async Task Revoke(string enrollmentId, CancellationToken cancellationToken = default)
    {
        var service = await GetService(cancellationToken);
        await service.RevokeFederationEnrollment(new() { Id = enrollmentId }, CallContextFor(cancellationToken)).EnsureSuccess();
    }

    /// <inheritdoc/>
    public async Task<FederationRedemption> Redeem(string code, CancellationToken cancellationToken = default)
    {
        var service = await GetService(cancellationToken);
        var response = await service.RedeemFederationEnrollment(new() { Code = code }, CallContextFor(cancellationToken)).EnsureSuccess();
        return new(response.Identity, response.Serial, response.NotAfter, response.Endpoint, response.AlreadyEnrolled);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<FederationEnrollmentInformation>> GetEnrollments(CancellationToken cancellationToken = default)
    {
        var service = await GetService(cancellationToken);
        var enrollments = await service.AllEnrollments(CallContextFor(cancellationToken)).EnsureSuccess();
        return [.. enrollments.Select(_ => new FederationEnrollmentInformation(_.Id, _.Subject, _.Instance, _.CreatedAt, _.ExpiresAt, _.CreatedBy, _.Status))];
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<FederationCertificateInformation>> GetIssuedCertificates(CancellationToken cancellationToken = default)
    {
        var service = await GetService(cancellationToken);
        var certificates = await service.AllIssuedCertificates(CallContextFor(cancellationToken)).EnsureSuccess();
        return [.. certificates.Select(_ => new FederationCertificateInformation(_.Serial, _.EnrollmentId, _.Subject, _.Instance, _.Identity, _.NotBefore, _.NotAfter, _.Reason, _.Status))];
    }

    /// <inheritdoc/>
    public async Task<FederationEnrollmentStatus> GetStatus(CancellationToken cancellationToken = default)
    {
        var service = await GetService(cancellationToken);
        var status = await service.CurrentEnrollmentStatus(CallContextFor(cancellationToken)).EnsureSuccess();
        return new(
            status.Enrolled,
            status.Identity,
            status.Serial,
            status.Enrolled ? status.NotAfter : null,
            status.Enrolled ? status.RenewAfter : null,
            status.Endpoint,
            status.AuthorityFingerprint,
            status.Expired);
    }

    static CallContext CallContextFor(CancellationToken cancellationToken) => new(new CallOptions(cancellationToken: cancellationToken));

    async Task<Contracts.Federation.IFederation> GetService(CancellationToken cancellationToken) =>
        (await _servicesAccessor.GetServices(cancellationToken)).Federation;
}
