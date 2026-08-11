// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Cratis.Chronicle.Aspire.for_ChronicleDistributedApplicationBuilderExtensions.when_adding_chronicle;

public class and_configuring_for_production : given.a_distributed_application_builder
{
    IResourceBuilder<ChronicleResource> _result;
    IChronicleAspireBuilder _configured;
    ContainerImageAnnotation _image;

    void Because()
    {
        _result = _builder.AddCratisChronicle(configure: chronicle => _configured = chronicle);
        _image = _result.Resource.Annotations.OfType<ContainerImageAnnotation>().Single();
    }

    [Fact] void should_use_the_production_image_tag() => _image.Tag.ShouldEqual(ChronicleContainerImageTags.Tag);
    [Fact] void should_not_use_the_development_slim_image_tag() => _image.Tag.ShouldNotEqual(ChronicleContainerImageTags.DevelopmentSlimTag);
    [Fact] void should_use_the_chronicle_image() => _image.Image.ShouldEqual(ChronicleContainerImageTags.Image);
    [Fact] void should_invoke_the_configure_callback() => _configured.ShouldNotBeNull();
    [Fact] void should_hand_the_chronicle_resource_to_the_callback() => _configured.ResourceBuilder.Resource.ShouldEqual(_result.Resource);
}
