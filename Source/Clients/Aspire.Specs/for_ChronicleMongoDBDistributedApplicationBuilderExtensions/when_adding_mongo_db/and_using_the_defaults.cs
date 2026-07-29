// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Cratis.Chronicle.Aspire.for_ChronicleMongoDBDistributedApplicationBuilderExtensions.when_adding_mongo_db;

public class and_using_the_defaults : given.a_distributed_application_builder
{
    IResourceBuilder<IResourceWithConnectionString> _result;
    ContainerResource _server;
    ContainerImageAnnotation _image;
    EndpointAnnotation _endpoint;
    IEnumerable<string> _arguments;

    async Task Because()
    {
        _result = _builder.AddCratisChronicleMongoDB();
        _server = _builder.Resources.OfType<ContainerResource>().Single(_ => _.Name == "mongodb-server");
        _image = _server.Annotations.OfType<ContainerImageAnnotation>().Single();
        _endpoint = _server.Annotations.OfType<EndpointAnnotation>().Single();
        _arguments = await ArgumentsFor(_server);
    }

    [Fact] void should_name_the_connection_string_resource_by_convention() => _result.Resource.Name.ShouldEqual("mongodb");
    [Fact] void should_use_the_mongo_db_image() => _image.Image.ShouldEqual(ChronicleContainerImageTags.MongoDBImage);
    [Fact] void should_use_the_default_image_tag() => _image.Tag.ShouldEqual(ChronicleContainerImageTags.MongoDBTag);
    [Fact] void should_use_the_docker_hub_registry() => _image.Registry.ShouldEqual(ChronicleContainerImageTags.Registry);
    [Fact] void should_name_the_endpoint_by_convention() => _endpoint.Name.ShouldEqual(ChronicleContainerImageTags.MongoDBEndpointName);
    [Fact] void should_target_the_mongo_db_port() => _endpoint.TargetPort.ShouldEqual(27017);
    [Fact] void should_run_the_command_through_a_shell() => _server.Entrypoint.ShouldEqual("/bin/sh");
    [Fact] void should_pass_the_command_to_the_shell() => _arguments.First().ShouldEqual("-c");
    [Fact] void should_start_mongod_as_a_replica_set() => _arguments.Last().ShouldContain($"mongod --replSet {ChronicleContainerImageTags.MongoDBReplicaSetName} --bind_ip_all");
    [Fact] void should_initiate_the_replica_set() => _arguments.Last().ShouldContain($"rs.initiate({{ _id: \"{ChronicleContainerImageTags.MongoDBReplicaSetName}\"");
    [Fact] void should_keep_mongod_as_the_container_entrypoint_process() => _arguments.Last().ShouldContain("exec docker-entrypoint.sh");
    [Fact] void should_connect_directly_to_the_single_node() => _result.Resource.ConnectionStringExpression.ValueExpression.ShouldContain("directConnection=true");
    [Fact] void should_connect_through_the_mongo_db_endpoint() => _result.Resource.ConnectionStringExpression.ValueExpression.ShouldContain($"mongodb://{{mongodb-server.bindings.{ChronicleContainerImageTags.MongoDBEndpointName}.host}}:{{mongodb-server.bindings.{ChronicleContainerImageTags.MongoDBEndpointName}.port}}/");
}
