// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_ChronicleFixture;

[Collection("External MongoDB fixture environment")]
public class when_starting_without_containers : Specification
{
    string? _originalConnectionString;
    ExternalFixture _fixture;

    void Establish()
    {
        _originalConnectionString = Environment.GetEnvironmentVariable("CHRONICLE_MONGODB_CONNECTION_DETAILS");
        Environment.SetEnvironmentVariable("CHRONICLE_MONGODB_CONNECTION_DETAILS", "mongodb://localhost:27017");
    }

    void Because() => _fixture = new ExternalFixture();

    [Fact] void should_start_without_requiring_docker() => _fixture.ShouldNotBeNull();
    [Fact] void should_use_the_external_connection_string() => _fixture.MongoDBConnectionString.ShouldEqual("mongodb://localhost:27017");

    async Task Destroy()
    {
        Environment.SetEnvironmentVariable("CHRONICLE_MONGODB_CONNECTION_DETAILS", _originalConnectionString);

        // Startup creates no database clients or containers. Only the logger factory needs cleanup;
        // disposing the whole fixture would contact MongoDB to drop databases that this spec never created.
        await (_fixture?.DisposeAsync() ?? ValueTask.CompletedTask);
    }

    class ExternalFixture : ChronicleInProcessFixture
    {
        public override ValueTask DisposeAsync()
        {
            LoggerFactory.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
