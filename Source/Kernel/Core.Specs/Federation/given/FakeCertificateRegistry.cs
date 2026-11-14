// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.given;

/// <summary>
/// An <see cref="IFederationCertificateRegistry"/> that folds what is appended to it, and remembers every event.
/// </summary>
public class FakeCertificateRegistry : IFederationCertificateRegistry
{
    readonly FederationCertificateHistory _history = new();
    readonly HashSet<string> _claimed = [];

    /// <summary>
    /// Gets or sets something to wait for after an enrollment was read, to make concurrent callers interleave.
    /// </summary>
    public Func<Task>? OnFindEnrollment { get; set; }

    /// <summary>
    /// Gets every event appended, in order.
    /// </summary>
    public List<(string EventSourceId, object Event)> Appended { get; } = [];

    /// <inheritdoc/>
    public Task<IReadOnlyList<FederationEnrollmentState>> GetEnrollments() => Task.FromResult(_history.Enrollments);

    /// <inheritdoc/>
    public async Task<FederationEnrollmentState?> FindEnrollment(string id)
    {
        var state = _history.FindEnrollment(id);
        if (OnFindEnrollment is not null)
        {
            // The state was read before the wait, so a caller that waits here decides on what was true before the others got in.
            await OnFindEnrollment();
        }

        return state;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<FederationIssuedCertificateState>> GetIssuedCertificates() => Task.FromResult(_history.Certificates);

    /// <inheritdoc/>
    public Task<FederationIssuedCertificateState?> FindIssuedCertificate(string serial) => Task.FromResult(_history.FindCertificate(serial));

    /// <inheritdoc/>
    public Task<IReadOnlyList<FederationPolicyState>> GetPolicies() => Task.FromResult(_history.Policies);

    /// <inheritdoc/>
    public Task<FederationPolicyState?> FindPolicy(string subject) => Task.FromResult(_history.FindPolicy(subject));

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

    /// <inheritdoc/>
    public Task<bool> ClaimRedemption(string enrollmentId, string serial)
    {
        lock (_claimed)
        {
            if (!_claimed.Add(enrollmentId))
            {
                return Task.FromResult(false);
            }
        }

        return Append(enrollmentId, new FederationEnrollmentRedeemed(serial)).ContinueWith(_ => true, TaskScheduler.Default);
    }
}
