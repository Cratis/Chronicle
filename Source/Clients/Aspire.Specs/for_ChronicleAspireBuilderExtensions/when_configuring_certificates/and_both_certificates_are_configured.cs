// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Cratis.Chronicle.Aspire.for_ChronicleAspireBuilderExtensions.when_configuring_certificates;

public class and_both_certificates_are_configured : given.a_distributed_application_builder
{
    const string TlsCertificatePath = "certs/chronicle.pfx";
    const string TlsCertificatePassword = "the-tls-password";
    const string EncryptionCertificatePath = "certs/encryption.pfx";
    const string EncryptionCertificatePassword = "the-encryption-password";

    IResourceBuilder<ChronicleResource> _result;
    Dictionary<string, object> _environment;
    ContainerMountAnnotation _tlsMount;
    ContainerMountAnnotation _encryptionMount;

    async Task Because()
    {
        _result = _builder.AddCratisChronicle(configure: chronicle => chronicle
            .WithTlsCertificate(TlsCertificatePath, TlsCertificatePassword)
            .WithEncryptionCertificate(EncryptionCertificatePath, EncryptionCertificatePassword));
        _environment = await EnvironmentFor(_result.Resource);
        _tlsMount = MountsFor(_result.Resource).Single(_ => _.Target == ChronicleContainerImageTags.TlsCertificateContainerPath);
        _encryptionMount = MountsFor(_result.Resource).Single(_ => _.Target == ChronicleContainerImageTags.EncryptionCertificateContainerPath);
    }

    [Fact] void should_mount_one_file_per_certificate() => MountsFor(_result.Resource).Count().ShouldEqual(2);
    [Fact] void should_mount_the_tls_certificate_from_its_host_path() => _tlsMount.Source.ShouldEqual(Path.GetFullPath(TlsCertificatePath));
    [Fact] void should_mount_the_encryption_certificate_from_its_host_path() => _encryptionMount.Source.ShouldEqual(Path.GetFullPath(EncryptionCertificatePath));
    [Fact] void should_point_the_tls_certificate_path_at_its_mounted_file() => _environment[ChronicleContainerImageTags.TlsCertificatePathEnvironmentVariable].ShouldEqual(_tlsMount.Target);
    [Fact] void should_point_the_encryption_certificate_path_at_its_mounted_file() => _environment[ChronicleContainerImageTags.EncryptionCertificatePathEnvironmentVariable].ShouldEqual(_encryptionMount.Target);
    [Fact] void should_configure_the_tls_certificate_password() => _environment[ChronicleContainerImageTags.TlsCertificatePasswordEnvironmentVariable].ShouldEqual(TlsCertificatePassword);
    [Fact] void should_configure_the_encryption_certificate_password() => _environment[ChronicleContainerImageTags.EncryptionCertificatePasswordEnvironmentVariable].ShouldEqual(EncryptionCertificatePassword);
}
