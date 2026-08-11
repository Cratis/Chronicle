// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Cratis.Chronicle.Aspire.for_ChronicleAspireBuilderExtensions.when_configuring_certificates;

public class and_the_tls_certificate_is_configured_twice : given.a_distributed_application_builder
{
    const string FirstCertificatePath = "certs/shared.pfx";
    const string FirstCertificatePassword = "the-shared-password";
    const string SecondCertificatePath = "certs/environment.pfx";

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
            .WithTlsCertificate(FirstCertificatePath, FirstCertificatePassword)
            .WithTlsCertificate(SecondCertificatePath));
        _environment = await EnvironmentFor(_result.Resource);
        _mounts = MountsFor(_result.Resource, ChronicleContainerImageTags.TlsCertificateContainerPath);
    }

    [Fact] void should_mount_only_one_certificate_on_the_container_path() => _mounts.Length.ShouldEqual(1);
    [Fact] void should_mount_the_certificate_from_the_last_call() => _mounts.Single().Source.ShouldEqual(InAppHostDirectory(SecondCertificatePath));
    [Fact] void should_still_configure_the_certificate_path() => _environment[ChronicleContainerImageTags.TlsCertificatePathEnvironmentVariable].ShouldEqual(ChronicleContainerImageTags.TlsCertificateContainerPath);
    [Fact] void should_not_keep_the_password_from_the_overridden_call() => _environment.ContainsKey(ChronicleContainerImageTags.TlsCertificatePasswordEnvironmentVariable).ShouldBeFalse();
}
