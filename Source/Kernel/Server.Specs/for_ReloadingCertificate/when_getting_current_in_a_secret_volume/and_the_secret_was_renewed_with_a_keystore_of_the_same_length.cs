// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Security.Cryptography.X509Certificates;
namespace Cratis.Chronicle.Server.for_ReloadingCertificate.when_getting_current_in_a_secret_volume;

public class and_the_secret_was_renewed_with_a_keystore_of_the_same_length : given.a_kubernetes_secret_volume
{
    X509Certificate2 _result;

    protected override X509Certificate2 CreateRenewal() => given.TestCertificates.CreateWithSameLength("CN=fresh", _first);

    void Establish() => Publish(_second);

    void Because()
    {
        _time.Advance(TimeSpan.FromMinutes(1));
        _result = _certificate.Current;
    }

    [given.SymbolicLinkFact] void should_have_a_renewal_of_the_same_length() => given.TestCertificates.Export(_second).Length.ShouldEqual(given.TestCertificates.Export(_first).Length);
    [given.SymbolicLinkFact] void should_return_the_new_certificate() => _result.Thumbprint.ShouldEqual(_second.Thumbprint);
    [given.SymbolicLinkFact] void should_report_one_reload() => _reloads.ShouldEqual(1);
}
