// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Cratis.Chronicle.Aspire.for_ChronicleDistributedApplicationBuilderExtensions.when_adding_chronicle;

public class and_publishing : Specification
{
    IDistributedApplicationBuilder _builder;
    IResourceBuilder<ChronicleResource> _result;
    ContainerImageAnnotation _image;

    void Establish() => _builder = DistributedApplication.CreateBuilder(["--publisher", "manifest"]);

    void Because()
    {
        _result = _builder.AddCratisChronicle();
        _image = _result.Resource.Annotations.OfType<ContainerImageAnnotation>().Single();
    }

    [Fact] void should_use_the_production_image_tag() => _image.Tag.ShouldEqual(ChronicleContainerImageTags.Tag);
}
