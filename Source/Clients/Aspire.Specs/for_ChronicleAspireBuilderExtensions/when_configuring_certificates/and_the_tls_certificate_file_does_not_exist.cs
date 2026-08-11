// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aspire.Hosting;

namespace Cratis.Chronicle.Aspire.for_ChronicleAspireBuilderExtensions.when_configuring_certificates;

public class and_the_tls_certificate_file_does_not_exist : given.a_distributed_application_builder
{
    const string MissingCertificatePath = "certs/missing-tls.pfx";

    Exception _exception;

    void Because() => _exception = Catch.Exception(() =>
        _builder.AddCratisChronicle(configure: chronicle => chronicle.WithTlsCertificate(MissingCertificatePath)));

    [Fact] void should_fail_the_app_host() => _exception.ShouldBeOfExactType<CertificateFileDoesNotExist>();
    [Fact] void should_name_the_configured_path() => _exception.Message.ShouldContain(MissingCertificatePath);
    [Fact] void should_name_the_path_it_resolved_to() => _exception.Message.ShouldContain(InAppHostDirectory(MissingCertificatePath));
}
