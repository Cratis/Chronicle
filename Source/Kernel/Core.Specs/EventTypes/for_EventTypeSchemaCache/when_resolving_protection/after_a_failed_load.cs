// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_EventTypeSchemaCache.when_resolving_protection;

public class after_a_failed_load : given.a_protection_schema_cache
{
    Exception _failure;
    JsonSchema _result;

    async Task Establish()
    {
        _eventTypes.GetDefinition(_eventTypeId).Returns(Task.FromException<EventTypeDefinition>(new InvalidOperationException("lookup failed")), Task.FromResult(_definition));
        try
        {
            await Resolve();
        }
        catch (InvalidOperationException exception)
        {
            _failure = exception;
        }
    }

    async Task Because() => _result = await Resolve();

    [Fact] void should_surface_the_original_failure() => _failure.Message.ShouldEqual("lookup failed");
    [Fact] void should_not_poison_the_cache() => _result.HasSchemaMetadata().ShouldBeTrue();
    [Fact] void should_retry_the_definition_lookup() => DefinitionLookups().ShouldEqual(2);
}
