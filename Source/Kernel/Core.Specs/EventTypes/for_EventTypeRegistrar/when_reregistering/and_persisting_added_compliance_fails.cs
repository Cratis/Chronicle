// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_reregistering;

public class and_persisting_added_compliance_fails : given.a_registration
{
    ILogger<EventTypeRegistrar> _logger;
    Exception _exception;

    void Establish()
    {
        StoredEventTypes(StoredEventType("decimal", (1, Typed)));
        _logger = Substitute.For<ILogger<EventTypeRegistrar>>();
        _logger.IsEnabled(LogLevel.Warning).Returns(true);
        _subject = new EventTypeRegistrar(_grainFactory, _logger);
        _eventTypesStorage.Register(Arg.Any<IEnumerable<EventTypeToRegister>>()).Returns(Task.FromException<IEnumerable<EventTypeId>>(new StorageFailure()));
    }

    async Task Because() => _exception = await Catch.Exception(() => Register(Protected));

    [Fact] void should_fail_the_registration() => _exception.ShouldBeOfExactType<StorageFailure>();
    [Fact] void should_not_claim_the_metadata_was_added() => _logger.ReceivedCalls().Any(call => call.GetMethodInfo().Name == "Log").ShouldBeFalse();

    class StorageFailure : Exception;
}
