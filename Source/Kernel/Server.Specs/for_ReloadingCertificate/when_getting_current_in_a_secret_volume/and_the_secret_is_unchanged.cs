// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Security.Cryptography.X509Certificates;
namespace Cratis.Chronicle.Server.for_ReloadingCertificate.when_getting_current_in_a_secret_volume;

public class and_the_secret_is_unchanged : given.a_kubernetes_secret_volume
{
    X509Certificate2 _result;

    void Because()
    {
        _time.Advance(TimeSpan.FromMinutes(1));
        _result = _certificate.Current;
    }

    [given.SymbolicLinkFact] void should_keep_the_current_certificate() => _result.Thumbprint.ShouldEqual(_first.Thumbprint);
    [given.SymbolicLinkFact] void should_not_reload() => _reloads.ShouldEqual(0);
    [given.SymbolicLinkFact] void should_not_report_a_failure() => _failures.ShouldEqual(0);
}
