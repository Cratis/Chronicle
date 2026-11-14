// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationEnrollmentStatus.given;

public class an_enrolled_kernel_with_a_dialer_session : Specification
{
    protected static readonly DateTimeOffset _start = new(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);

    protected MutableTimeProvider _time;
    protected FederationStatus _dial;
    protected IFederationCertificateRegistry _registry;

    void Establish()
    {
        _time = new(_start);
        _dial = new(_time);
        _registry = Substitute.For<IFederationCertificateRegistry>();
        _registry.GetInstalledIdentity().Returns(Task.FromResult<FederationIdentityInstalled?>(
            new FederationIdentityInstalled("acme", "01", _start.AddDays(-1), _start.AddDays(29), "https://authority.example", "abcdef", string.Empty, string.Empty, string.Empty, "Enrollment")));
    }

    protected Task<FederationEnrollmentStatus> Read() => FederationEnrollmentStatus.CurrentEnrollmentStatus(_registry, _time, _dial);

    protected sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        DateTimeOffset _now = now;

        public void Advance(TimeSpan by) => _now += by;

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
