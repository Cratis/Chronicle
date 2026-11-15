// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Security.for_EventStoreAccessConfigurationValidation;

public class when_validating_an_external_issuer_with_the_internal_authority : Specification
{
    ILogger<EventStoreAccessConfigurationValidation> _logger;
    bool _succeeded;

    void Establish()
    {
        _logger = Substitute.For<ILogger<EventStoreAccessConfigurationValidation>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
    }

    void Because()
    {
        var options = new ChronicleOptions
        {
            Authentication = new()
            {
                EventStoreAccess =
                [
                    new() { Issuer = "external", Subject = "worker", EventStore = "orders", Access = StoreAccess.Read },
                    new() { Issuer = PrincipalKey.LocalIssuer, Subject = "worker", EventStore = "orders", Access = StoreAccess.Read }
                ]
            }
        };
        _succeeded = new EventStoreAccessConfigurationValidation(_logger).Validate(null, options).Succeeded;
    }

    [Fact] void should_still_accept_the_configuration() => _succeeded.ShouldBeTrue();
    [Fact] void should_warn_once_about_the_issuer_that_never_matches() => _logger.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(ILogger.Log)).ShouldEqual(1);
}
