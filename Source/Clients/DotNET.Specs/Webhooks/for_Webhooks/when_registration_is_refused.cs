// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Observation.Webhooks;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;
using ClientWebhooks = Cratis.Chronicle.Webhooks.Webhooks;
using ContractWebhooks = Cratis.Chronicle.Contracts.Observation.Webhooks.IWebhooks;

namespace Cratis.Chronicle.Specs.Webhooks.for_Webhooks;

public class when_registration_is_refused : Specification
{
    ClientWebhooks _webhooks;
    ILogger<ClientWebhooks> _logger;
    CommandResult _result;
    Exception _exception;

    void Establish()
    {
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        var accessor = (IChronicleServicesAccessor)connection;
        accessor.Services.Returns(Substitute.For<IServices>());
        var service = Substitute.For<ContractWebhooks>();
        accessor.Services.Webhooks.Returns(service);
        _result = new CommandResult { ExceptionMessages = ["Observer is already served by another transport."] };
        service.AddWebhooks(Arg.Any<AddWebhooksRequest>()).Returns(_result);
        var eventStore = Substitute.For<IEventStore>();
        eventStore.Name.Returns(new EventStoreName("some-event-store"));
        eventStore.Connection.Returns(connection);
        _logger = Substitute.For<ILogger<ClientWebhooks>>();
        _logger.IsEnabled(LogLevel.Warning).Returns(true);
        _webhooks = new ClientWebhooks(Substitute.For<IEventTypes>(), eventStore, _logger);
    }

    async Task Because() => _exception = await Catch.Exception(() => _webhooks.Register("the-webhook", "https://example.com/webhook", _ => { }));

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_log_one_warning() => _logger.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(ILogger.Log) && Equals(call.GetArguments()[0], LogLevel.Warning)).ShouldEqual(1);
    [Fact] void should_include_the_failure_details() => _logger.ReceivedCalls().Single(call => call.GetMethodInfo().Name == nameof(ILogger.Log)).GetArguments()[2].ToString().ShouldContain(_result.ExceptionMessages.Single());
}
