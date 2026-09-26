// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Cratis.Chronicle.Aspire.for_ChronicleDistributedApplicationBuilderExtensions.when_adding_chronicle;

public class and_specifying_a_name_and_configuring_storage_in_run_mode : given.a_distributed_application_builder
{
    const string Name = "event-store";

    IResourceBuilder<ChronicleResource> _result;
    IChronicleAspireBuilder _configured;
    ContainerImageAnnotation _image;

    void Because()
    {
        _result = _builder.AddCratisChronicle(Name, chronicle => _configured = chronicle.WithSqlite("Data Source=/data/chronicle.db"));
        _image = _result.Resource.Annotations.OfType<ContainerImageAnnotation>().Single();
    }

    [Fact] void should_name_the_resource_as_specified() => _result.Resource.Name.ShouldEqual(Name);
    [Fact] void should_use_the_development_image_tag() => _image.Tag.ShouldEqual(ChronicleContainerImageTags.DevelopmentTag);
    [Fact] void should_hand_the_named_chronicle_resource_to_the_callback() => _configured.ResourceBuilder.Resource.Name.ShouldEqual(Name);
}
