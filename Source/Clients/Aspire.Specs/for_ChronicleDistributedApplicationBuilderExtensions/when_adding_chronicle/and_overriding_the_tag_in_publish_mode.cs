// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Cratis.Chronicle.Aspire.for_ChronicleDistributedApplicationBuilderExtensions.when_adding_chronicle;

public class and_overriding_the_tag_in_publish_mode : Specification
{
    const string ImageTag = "19.0.0-development-slim";

    IDistributedApplicationBuilder _builder;
    ContainerImageAnnotation _image;

    void Establish() => _builder = DistributedApplication.CreateBuilder(["--publisher", "manifest"]);

    void Because()
    {
        var result = _builder.AddCratisChronicle().WithImageTag(ImageTag);
        _image = result.Resource.Annotations.OfType<ContainerImageAnnotation>().Single();
    }

    [Fact] void should_use_the_explicit_image_tag() => _image.Tag.ShouldEqual(ImageTag);
}
