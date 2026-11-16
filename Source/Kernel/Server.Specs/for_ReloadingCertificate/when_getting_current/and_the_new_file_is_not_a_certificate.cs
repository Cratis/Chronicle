// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Security.Cryptography.X509Certificates;
namespace Cratis.Chronicle.Server.for_ReloadingCertificate.when_getting_current;

public class and_the_new_file_is_not_a_certificate : given.a_reloading_certificate
{
    X509Certificate2 _result;

    void Establish()
    {
        File.WriteAllText(_path, "half written");
        File.SetLastWriteTimeUtc(_path, DateTime.UtcNow.AddDays(1));
    }

    void Because()
    {
        _time.Advance(TimeSpan.FromMinutes(1));
        _result = _certificate.Current;
        _time.Advance(TimeSpan.FromMinutes(1));
        _ = _certificate.Current;
    }

    [Fact] void should_keep_the_current_certificate() => _result.Thumbprint.ShouldEqual(_first.Thumbprint);
    [Fact] void should_report_the_failure_once() => _failures.ShouldEqual(1);
}
