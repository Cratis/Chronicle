// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Cratis.Chronicle.Aspire.for_ChronicleAspireBuilderExtensions.when_configuring_certificates;

public class and_no_certificate_is_configured : given.a_distributed_application_builder
{
    const string ConnectionString = "Data Source=/data/chronicle.db";

    IResourceBuilder<ChronicleResource> _result;
    Dictionary<string, object> _environment;
    IEnumerable<ContainerMountAnnotation> _mounts;

    async Task Because()
    {
        _result = _builder.AddCratisChronicle(configure: chronicle => chronicle.WithSqlite(ConnectionString));
        _environment = await EnvironmentFor(_result.Resource);
        _mounts = MountsFor(_result.Resource);
    }

    [Fact] void should_still_configure_the_storage() => _environment[ChronicleContainerImageTags.StorageConnectionDetailsEnvironmentVariable].ShouldEqual(ConnectionString);
    [Fact] void should_not_configure_a_tls_certificate_path() => _environment.ContainsKey(ChronicleContainerImageTags.TlsCertificatePathEnvironmentVariable).ShouldBeFalse();
    [Fact] void should_not_configure_a_tls_certificate_password() => _environment.ContainsKey(ChronicleContainerImageTags.TlsCertificatePasswordEnvironmentVariable).ShouldBeFalse();
    [Fact] void should_not_configure_an_encryption_certificate_path() => _environment.ContainsKey(ChronicleContainerImageTags.EncryptionCertificatePathEnvironmentVariable).ShouldBeFalse();
    [Fact] void should_not_configure_an_encryption_certificate_password() => _environment.ContainsKey(ChronicleContainerImageTags.EncryptionCertificatePasswordEnvironmentVariable).ShouldBeFalse();
    [Fact] void should_not_mount_anything_into_the_container() => _mounts.ShouldBeEmpty();
}
