// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Contracts.ExternalServices;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.ExternalServices.for_AddExternalService.when_validating;

public class and_host_is_missing_for_a_database_endpoint : Specification
{
    readonly CommandScenario<AddExternalService> _scenario = new();
    CommandResult _result;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore(Arg.Any<EventStoreName>()).Returns(true);
        _scenario.Services.AddSingleton(storage);
    }

    async Task Because() => _result = await _scenario.Validate(new AddExternalService("some-event-store", "some-id", "some-service", ExternalServiceEndpointType.PostgreSql, string.Empty, AuthorizationType.None, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, new Dictionary<string, string>(), string.Empty, 5432, "some-database", string.Empty, string.Empty, new Dictionary<string, string>()));

    [Fact] void should_not_be_successful() => _result.ShouldNotBeSuccessful();
    [Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();
}
