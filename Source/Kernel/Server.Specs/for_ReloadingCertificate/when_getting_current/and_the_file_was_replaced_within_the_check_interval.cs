// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Security.Cryptography.X509Certificates;
namespace Cratis.Chronicle.Server.for_ReloadingCertificate.when_getting_current;

public class and_the_file_was_replaced_within_the_check_interval : given.a_reloading_certificate
{
    X509Certificate2 _result;

    void Establish() => Write(_second);

    void Because()
    {
        _time.Advance(TimeSpan.FromSeconds(5));
        _result = _certificate.Current;
    }

    [Fact] void should_still_return_the_initial_certificate() => _result.Thumbprint.ShouldEqual(_first.Thumbprint);
}
