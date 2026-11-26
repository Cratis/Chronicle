// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Security.Cryptography.X509Certificates;
namespace Cratis.Chronicle.Server.for_ReloadingCertificate.when_getting_current_in_a_secret_volume;

public class and_the_secret_was_renewed_with_a_broken_keystore : given.a_kubernetes_secret_volume
{
    X509Certificate2 _result;

    void Establish()
    {
        var broken = Path.Combine(_volume, "..broken");
        Directory.CreateDirectory(broken);
        File.WriteAllText(Path.Combine(broken, "keystore.p12"), "half written");
        Directory.Delete(Path.Combine(_volume, "..data"));
        Directory.CreateSymbolicLink(Path.Combine(_volume, "..data"), "..broken");
    }

    void Because()
    {
        _time.Advance(TimeSpan.FromMinutes(1));
        _result = _certificate.Current;
        _time.Advance(TimeSpan.FromMinutes(1));
        _ = _certificate.Current;
    }

    [given.SymbolicLinkFact] void should_keep_the_current_certificate() => _result.Thumbprint.ShouldEqual(_first.Thumbprint);
    [given.SymbolicLinkFact] void should_report_the_failure_once() => _failures.ShouldEqual(1);
}
