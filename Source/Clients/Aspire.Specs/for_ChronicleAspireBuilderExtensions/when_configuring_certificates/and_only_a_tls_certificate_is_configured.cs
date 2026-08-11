// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Cratis.Chronicle.Aspire.for_ChronicleAspireBuilderExtensions.when_configuring_certificates;

public class and_only_a_tls_certificate_is_configured : given.a_distributed_application_builder
{
    const string CertificatePath = "certs/chronicle.pfx";
    const string CertificatePassword = "the-tls-password";

    IResourceBuilder<ChronicleResource> _result;
    Dictionary<string, object> _environment;
    ContainerMountAnnotation _mount;

    async Task Because()
    {
        _result = _builder.AddCratisChronicle(configure: chronicle => chronicle.WithTlsCertificate(CertificatePath, CertificatePassword));
        _environment = await EnvironmentFor(_result.Resource);
        _mount = MountsFor(_result.Resource).Single();
    }

    [Fact] void should_mount_the_certificate_from_the_given_host_path() => _mount.Source.ShouldEqual(Path.GetFullPath(CertificatePath));
    [Fact] void should_mount_the_certificate_at_the_tls_certificate_container_path() => _mount.Target.ShouldEqual(ChronicleContainerImageTags.TlsCertificateContainerPath);
    [Fact] void should_mount_the_certificate_as_read_only() => _mount.IsReadOnly.ShouldBeTrue();
    [Fact] void should_mount_the_certificate_as_a_bind_mount() => _mount.Type.ShouldEqual(ContainerMountType.BindMount);
    [Fact] void should_point_the_configured_certificate_path_at_the_mounted_file() => _environment[ChronicleContainerImageTags.TlsCertificatePathEnvironmentVariable].ShouldEqual(_mount.Target);
    [Fact] void should_configure_the_certificate_password() => _environment[ChronicleContainerImageTags.TlsCertificatePasswordEnvironmentVariable].ShouldEqual(CertificatePassword);
    [Fact] void should_not_configure_an_encryption_certificate_path() => _environment.ContainsKey(ChronicleContainerImageTags.EncryptionCertificatePathEnvironmentVariable).ShouldBeFalse();
    [Fact] void should_not_configure_an_encryption_certificate_password() => _environment.ContainsKey(ChronicleContainerImageTags.EncryptionCertificatePasswordEnvironmentVariable).ShouldBeFalse();
}
