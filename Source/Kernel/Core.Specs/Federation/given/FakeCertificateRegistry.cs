// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.given;

/// <summary>
/// An <see cref="IFederationCertificateRegistry"/> that folds what is appended to it, and remembers every event.
/// </summary>
public class FakeCertificateRegistry : IFederationCertificateRegistry
{
    readonly FederationCertificateHistory _history = new();

    /// <summary>
    /// Gets every event appended, in order.
    /// </summary>
    public List<(string EventSourceId, object Event)> Appended { get; } = [];

    /// <inheritdoc/>
    public Task<IReadOnlyList<FederationEnrollmentState>> GetEnrollments() => Task.FromResult(_history.Enrollments);

    /// <inheritdoc/>
    public Task<FederationEnrollmentState?> FindEnrollment(string id) => Task.FromResult(_history.FindEnrollment(id));

    /// <inheritdoc/>
    public Task<IReadOnlyList<FederationIssuedCertificateState>> GetIssuedCertificates() => Task.FromResult(_history.Certificates);

    /// <inheritdoc/>
    public Task<FederationIssuedCertificateState?> FindIssuedCertificate(string serial) => Task.FromResult(_history.FindCertificate(serial));

    /// <inheritdoc/>
    public Task<FederationAuthorityCreated?> GetAuthority() => Task.FromResult(_history.Authority);

    /// <inheritdoc/>
    public Task<FederationIdentityInstalled?> GetInstalledIdentity() => Task.FromResult(_history.Identity);

    /// <inheritdoc/>
    public Task Append(string eventSourceId, object @event)
    {
        Appended.Add((eventSourceId, @event));
        _history.Apply(eventSourceId, @event);
        return Task.CompletedTask;
    }
}
