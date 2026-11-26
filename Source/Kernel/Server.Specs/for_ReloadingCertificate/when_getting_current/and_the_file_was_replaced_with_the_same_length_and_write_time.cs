// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Security.Cryptography.X509Certificates;
namespace Cratis.Chronicle.Server.for_ReloadingCertificate.when_getting_current;

public class and_the_file_was_replaced_with_the_same_length_and_write_time : given.a_reloading_certificate
{
    X509Certificate2 _renewed;
    X509Certificate2 _result;

    void Establish()
    {
        _renewed = given.TestCertificates.CreateWithSameLength("CN=fresh", _first);
        var written = File.GetLastWriteTimeUtc(_path);
        File.WriteAllBytes(_path, given.TestCertificates.Export(_renewed));
        File.SetLastWriteTimeUtc(_path, written);
    }

    void Because()
    {
        _time.Advance(TimeSpan.FromMinutes(1));
        _result = _certificate.Current;
    }

    void Destroy() => _renewed.Dispose();

    [Fact] void should_return_the_new_certificate() => _result.Thumbprint.ShouldEqual(_renewed.Thumbprint);
    [Fact] void should_report_one_reload() => _reloads.ShouldEqual(1);
}
