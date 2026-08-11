// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Cratis.Chronicle.Aspire.for_ChronicleAspireBuilderExtensions.when_configuring_certificates;

/// <summary>
/// A decoy file with the same relative path sits in the process working directory, so the mount can only land on
/// the AppHost directory copy by resolving against the AppHost directory rather than the working directory.
/// </summary>
public class and_the_certificate_path_is_relative : given.a_distributed_application_builder
{
    const string CertificatePath = "certs-relative-probe/chronicle.pfx";

    IResourceBuilder<ChronicleResource> _result;
    ContainerMountAnnotation _mount;

    void Establish()
    {
        CertificateFileIn(CertificatePath);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(CertificatePath))!);
        File.WriteAllText(Path.GetFullPath(CertificatePath), "The decoy in the working directory - the mount must not land here.");
    }

    void Because()
    {
        _result = _builder.AddCratisChronicle(configure: chronicle => chronicle.WithTlsCertificate(CertificatePath));
        _mount = MountsFor(_result.Resource).Single();
    }

    void Destroy() => Directory.Delete(Path.GetDirectoryName(Path.GetFullPath(CertificatePath))!, recursive: true);

    [Fact] void should_resolve_the_path_against_the_app_host_directory() => _mount.Source.ShouldEqual(InAppHostDirectory(CertificatePath));
}
