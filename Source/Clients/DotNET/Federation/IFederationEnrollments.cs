// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Defines the client API for hosted federation enrollment: how an operator enrolls a customer kernel with a one-time code,
/// and how a customer kernel redeems that code and reports its enrollment status.
/// </summary>
/// <remarks>
/// <para>
/// Get it from <see cref="IChronicleClient.Federation"/>. The kernel the client is connected to is the kernel that is acted on:
/// <see cref="Create"/>, <see cref="Revoke"/>, <see cref="GetEnrollments"/> and <see cref="GetIssuedCertificates"/> are for the
/// kernel that is the federation authority, and <see cref="Redeem"/> and <see cref="GetStatus"/> are for a kernel that is enrolled
/// with one. All of them need administrator rights on that kernel.
/// </para>
/// <para>
/// A failed command throws. The enrollment code is a one-time secret: the kernel only shows it in the result of <see cref="Create"/>.
/// </para>
/// </remarks>
public interface IFederationEnrollments
{
    /// <summary>
    /// Create a single-use, expiring enrollment for a subject on the authority kernel.
    /// </summary>
    /// <param name="subject">The opaque subject the enrollment is for, chosen by the operator. It becomes the customer part of the identity.</param>
    /// <param name="instance">Optional kernel instance the identity is issued for. A random one is generated when not given.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>The enrollment, with the code to hand to the customer.</returns>
    Task<FederationEnrollmentCode> Create(string subject, string? instance = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke an enrollment on the authority kernel, and every certificate issued from it, so the peer is refused at once.
    /// </summary>
    /// <param name="enrollmentId">The enrollment id.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Revoke(string enrollmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Redeem an enrollment code on the kernel that is connected to, so that it holds a certificate from the authority.
    /// It takes effect without a restart. It is idempotent when the kernel is already enrolled with the same authority.
    /// </summary>
    /// <param name="code">The enrollment code handed over by the operator.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>The identity the kernel now holds.</returns>
    Task<FederationRedemption> Redeem(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the enrollments of the authority kernel.
    /// </summary>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>The enrollments.</returns>
    Task<IEnumerable<FederationEnrollmentInformation>> GetEnrollments(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the registry of certificates the authority kernel issued.
    /// </summary>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>The issued certificates.</returns>
    Task<IEnumerable<FederationCertificateInformation>> GetIssuedCertificates(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the enrollment status of the kernel that is connected to.
    /// </summary>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>The status.</returns>
    Task<FederationEnrollmentStatus> GetStatus(CancellationToken cancellationToken = default);
}
