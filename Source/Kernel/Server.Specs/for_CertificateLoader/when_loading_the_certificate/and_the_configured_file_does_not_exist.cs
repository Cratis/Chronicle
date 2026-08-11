// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Server.for_CertificateLoader.when_loading_the_certificate;

public class and_the_configured_file_does_not_exist : given.a_certificate_file
{
    X509Certificate2 _result;
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => _result = CertificateLoader.LoadCertificate(OptionsWithTls(password: null)));

    [Fact] void should_not_load_a_certificate() => _result.ShouldBeNull();
    [Fact] void should_leave_it_to_the_caller_to_decide_what_a_missing_certificate_means() => _exception.ShouldBeNull();
}
