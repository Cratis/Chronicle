// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Cratis.Chronicle.Aspire.for_ChronicleAspireBuilderExtensions.when_configuring_certificates;

public class and_no_passwords_are_given : given.a_distributed_application_builder
{
    const string TlsCertificatePath = "certs/chronicle.pfx";
    const string EncryptionCertificatePath = "certs/encryption.pfx";

    IResourceBuilder<ChronicleResource> _result;
    Dictionary<string, object> _environment;

    async Task Because()
    {
        _result = _builder.AddCratisChronicle(configure: chronicle => chronicle
            .WithTlsCertificate(TlsCertificatePath)
            .WithEncryptionCertificate(EncryptionCertificatePath));
        _environment = await EnvironmentFor(_result.Resource);
    }

    [Fact] void should_configure_the_tls_certificate_path() => _environment[ChronicleContainerImageTags.TlsCertificatePathEnvironmentVariable].ShouldEqual(ChronicleContainerImageTags.TlsCertificateContainerPath);
    [Fact] void should_configure_the_encryption_certificate_path() => _environment[ChronicleContainerImageTags.EncryptionCertificatePathEnvironmentVariable].ShouldEqual(ChronicleContainerImageTags.EncryptionCertificateContainerPath);
    [Fact] void should_not_configure_a_tls_certificate_password() => _environment.ContainsKey(ChronicleContainerImageTags.TlsCertificatePasswordEnvironmentVariable).ShouldBeFalse();
    [Fact] void should_not_configure_an_encryption_certificate_password() => _environment.ContainsKey(ChronicleContainerImageTags.EncryptionCertificatePasswordEnvironmentVariable).ShouldBeFalse();
}
