// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_reregistering;

public class and_compliance_metadata_was_added : given.a_registration
{
    ILogger<EventTypeRegistrar> _logger;

    void Establish()
    {
        StoredEventTypes(StoredEventType("decimal", (1, Typed)));
        _logger = Substitute.For<ILogger<EventTypeRegistrar>>();
        _logger.IsEnabled(LogLevel.Warning).Returns(true);
        _subject = new EventTypeRegistrar(_grainFactory, _logger);
    }

    Task Because() => Register(Protected);

    [Fact] void should_keep_the_added_compliance() => _registered.Definition.Generations.Single().Schema.ActualProperties["amount"].GetComplianceMetadata().ShouldNotBeEmpty();
    [Fact] void should_warn_about_historical_plaintext() => _logger.ReceivedCalls().Any(call => call.GetMethodInfo().Name == "Log" && (LogLevel)call.GetArguments()[0]! == LogLevel.Warning).ShouldBeTrue();
}
