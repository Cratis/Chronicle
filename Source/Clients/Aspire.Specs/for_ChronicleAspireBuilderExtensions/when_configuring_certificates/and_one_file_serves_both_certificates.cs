// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Cratis.Chronicle.Aspire.for_ChronicleAspireBuilderExtensions.when_configuring_certificates;

public class and_one_file_serves_both_certificates : given.a_distributed_application_builder
{
    const string CertificatePath = "certs/chronicle.pfx";
    const string CertificatePassword = "the-shared-password";

    IResourceBuilder<ChronicleResource> _result;
    Dictionary<string, object> _environment;
    IEnumerable<ContainerMountAnnotation> _mounts;

    async Task Because()
    {
        _result = _builder.AddCratisChronicle(configure: chronicle => chronicle
            .WithTlsCertificate(CertificatePath, CertificatePassword)
            .WithEncryptionCertificate(CertificatePath, CertificatePassword));
        _environment = await EnvironmentFor(_result.Resource);
        _mounts = MountsFor(_result.Resource);
    }

    [Fact] void should_mount_the_same_host_file_twice() => _mounts.Select(_ => _.Source).ShouldContainOnly(Path.GetFullPath(CertificatePath), Path.GetFullPath(CertificatePath));
    [Fact] void should_mount_the_two_certificates_at_distinct_container_paths() => _mounts.Select(_ => _.Target).Distinct().Count().ShouldEqual(2);
    [Fact] void should_configure_the_tls_certificate_path() => _environment[ChronicleContainerImageTags.TlsCertificatePathEnvironmentVariable].ShouldEqual(ChronicleContainerImageTags.TlsCertificateContainerPath);
    [Fact] void should_configure_the_encryption_certificate_path() => _environment[ChronicleContainerImageTags.EncryptionCertificatePathEnvironmentVariable].ShouldEqual(ChronicleContainerImageTags.EncryptionCertificateContainerPath);
}
