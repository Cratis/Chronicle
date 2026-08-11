// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Cratis.Chronicle.Aspire.for_ChronicleDistributedApplicationBuilderExtensions.when_adding_chronicle;

public class and_using_the_defaults : given.a_distributed_application_builder
{
    IResourceBuilder<ChronicleResource> _result;
    ContainerImageAnnotation _image;
    EndpointAnnotation _endpoint;

    void Because()
    {
        _result = _builder.AddCratisChronicle();
        _image = _result.Resource.Annotations.OfType<ContainerImageAnnotation>().Single();
        _endpoint = _result.Resource.Annotations.OfType<EndpointAnnotation>().Single();
    }

    [Fact] void should_name_the_resource_by_convention() => _result.Resource.Name.ShouldEqual("chronicle");
    [Fact] void should_use_the_chronicle_image() => _image.Image.ShouldEqual(ChronicleContainerImageTags.Image);
    [Fact] void should_use_the_development_image_tag() => _image.Tag.ShouldEqual(ChronicleContainerImageTags.DevelopmentTag);
    [Fact] void should_use_the_docker_hub_registry() => _image.Registry.ShouldEqual(ChronicleContainerImageTags.Registry);
    [Fact] void should_name_the_endpoint_by_convention() => _endpoint.Name.ShouldEqual(ChronicleContainerImageTags.GrpcEndpointName);
    [Fact] void should_target_the_chronicle_port() => _endpoint.TargetPort.ShouldEqual(ChronicleResource.DefaultGrpcPort);
    [Fact] void should_expose_the_grpc_endpoint_as_the_connection_string() => _result.Resource.ConnectionStringExpression.ValueExpression.ShouldEqual($"chronicle://{{{_result.Resource.Name}.bindings.{ChronicleContainerImageTags.GrpcEndpointName}.host}}:{{{_result.Resource.Name}.bindings.{ChronicleContainerImageTags.GrpcEndpointName}.port}}");
}
