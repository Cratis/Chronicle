// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Cratis.Chronicle.Aspire.for_ChronicleAspireBuilderExtensions.when_configuring_certificates;

public class and_the_encryption_certificate_is_configured_twice : given.a_distributed_application_builder
{
    const string FirstCertificatePath = "certs/shared.pfx";
    const string SecondCertificatePath = "certs/environment.pfx";
    const string SecondCertificatePassword = "the-environment-password";

    IResourceBuilder<ChronicleResource> _result;
    Dictionary<string, object> _environment;
    ContainerMountAnnotation[] _mounts;

    void Establish()
    {
        CertificateFileIn(FirstCertificatePath);
        CertificateFileIn(SecondCertificatePath);
    }

    async Task Because()
    {
        _result = _builder.AddCratisChronicle(configure: chronicle => chronicle
            .WithEncryptionCertificate(FirstCertificatePath)
            .WithEncryptionCertificate(SecondCertificatePath, SecondCertificatePassword));
        _environment = await EnvironmentFor(_result.Resource);
        _mounts = MountsFor(_result.Resource, ChronicleContainerImageTags.EncryptionCertificateContainerPath);
    }

    [Fact] void should_mount_only_one_certificate_on_the_container_path() => _mounts.Length.ShouldEqual(1);
    [Fact] void should_mount_the_certificate_from_the_last_call() => _mounts.Single().Source.ShouldEqual(InAppHostDirectory(SecondCertificatePath));
    [Fact] void should_still_configure_the_certificate_path() => _environment[ChronicleContainerImageTags.EncryptionCertificatePathEnvironmentVariable].ShouldEqual(ChronicleContainerImageTags.EncryptionCertificateContainerPath);
    [Fact] void should_configure_the_password_from_the_last_call() => _environment[ChronicleContainerImageTags.EncryptionCertificatePasswordEnvironmentVariable].ShouldEqual(SecondCertificatePassword);
}
