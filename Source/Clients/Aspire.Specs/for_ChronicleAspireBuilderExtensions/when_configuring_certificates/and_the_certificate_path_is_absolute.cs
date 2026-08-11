// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Cratis.Chronicle.Aspire.for_ChronicleAspireBuilderExtensions.when_configuring_certificates;

public class and_the_certificate_path_is_absolute : given.a_distributed_application_builder
{
    string _certificatePath;
    IResourceBuilder<ChronicleResource> _result;
    ContainerMountAnnotation _mount;

    void Establish()
    {
        _certificatePath = Path.Combine(Path.GetTempPath(), $"chronicle-absolute-{Guid.NewGuid():N}.pfx");
        File.WriteAllText(_certificatePath, "A certificate outside the AppHost directory.");
    }

    void Because()
    {
        _result = _builder.AddCratisChronicle(configure: chronicle => chronicle.WithTlsCertificate(_certificatePath));
        _mount = MountsFor(_result.Resource).Single();
    }

    void Destroy() => File.Delete(_certificatePath);

    [Fact] void should_mount_the_certificate_from_the_absolute_path_as_given() => _mount.Source.ShouldEqual(_certificatePath);
}
