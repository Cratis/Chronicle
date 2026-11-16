// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Security.Cryptography.X509Certificates;
namespace Cratis.Chronicle.Server.for_ReloadingCertificate.when_getting_current;

public class and_the_file_is_unchanged : given.a_reloading_certificate
{
    X509Certificate2 _result;

    void Because()
    {
        _time.Advance(TimeSpan.FromMinutes(5));
        _result = _certificate.Current;
    }

    [Fact] void should_return_the_initial_certificate() => _result.Thumbprint.ShouldEqual(_first.Thumbprint);
    [Fact] void should_not_reload() => _reloads.ShouldEqual(0);
}
