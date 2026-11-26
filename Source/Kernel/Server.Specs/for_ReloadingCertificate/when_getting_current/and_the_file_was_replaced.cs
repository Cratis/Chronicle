// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Security.Cryptography.X509Certificates;
namespace Cratis.Chronicle.Server.for_ReloadingCertificate.when_getting_current;

public class and_the_file_was_replaced : given.a_reloading_certificate
{
    X509Certificate2 _result;

    void Establish() => Write(_second);

    void Because()
    {
        _time.Advance(TimeSpan.FromMinutes(1));
        _result = _certificate.Current;
    }

    [Fact] void should_return_the_new_certificate() => _result.Thumbprint.ShouldEqual(_second.Thumbprint);
    [Fact] void should_report_one_reload() => _reloads.ShouldEqual(1);
}
