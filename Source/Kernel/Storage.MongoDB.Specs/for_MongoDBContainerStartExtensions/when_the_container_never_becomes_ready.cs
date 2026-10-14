// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBContainerStartExtensions;

public class when_the_container_never_becomes_ready : Specification
{
    const int MongoDBPort = 27017;
    IContainer _container;
    Exception _error;

    void Establish() =>
        _container = new ContainerBuilder(Environment.GetEnvironmentVariable("CHRONICLE_SPECS_MONGODB_IMAGE") ?? "mongo:8.2")
            .WithCommand("/bin/sh", "-c", "echo never-ready; sleep infinity")
            .WithPortBinding(MongoDBPort, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(MongoDBPort))
            .Build();

    async Task Because() => _error = await Catch.Exception(() => _container.StartMongoDBWithDiagnostics(TimeSpan.FromSeconds(10)));

    async Task Destroy() => await _container.DisposeAsync();

    [Fact] void should_fail_with_a_start_timeout() => _error.ShouldBeOfExactType<MongoDBContainerStartTimedOut>();
    [Fact] void should_include_the_container_logs() => _error.Message.ShouldContain("never-ready");
}
