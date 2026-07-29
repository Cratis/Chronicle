// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Cratis.Chronicle.Aspire.for_ChronicleMongoDBDistributedApplicationBuilderExtensions.when_adding_mongo_db;

public class and_specifying_name_and_image_tag : given.a_distributed_application_builder
{
    const string Name = "chronicle-mongo";
    const string ImageTag = "7.0";

    IResourceBuilder<IResourceWithConnectionString> _result;
    ContainerResource _server;
    ContainerImageAnnotation _image;

    void Because()
    {
        _result = _builder.AddCratisChronicleMongoDB(Name, ImageTag);
        _server = _builder.Resources.OfType<ContainerResource>().Single(_ => _.Name == $"{Name}-server");
        _image = _server.Annotations.OfType<ContainerImageAnnotation>().Single();
    }

    [Fact] void should_name_the_connection_string_resource_as_specified() => _result.Resource.Name.ShouldEqual(Name);
    [Fact] void should_use_the_specified_image_tag() => _image.Tag.ShouldEqual(ImageTag);
    [Fact] void should_connect_through_the_named_server_endpoint() => _result.Resource.ConnectionStringExpression.ValueExpression.ShouldContain($"{Name}-server.bindings.{ChronicleContainerImageTags.MongoDBEndpointName}.host");
}
