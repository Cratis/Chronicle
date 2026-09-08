// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.EventSequences;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.for_InProcessCommandPipeline;

public class when_appending_from_a_consumer_command : Specification, IDisposable
{
    EventScenario _scenario;
    ServiceProvider _provider;
    IConsumerScope _consumerScope;
    EventSourceId _eventSourceId;
    CommandResult _result;

    void Establish()
    {
        _scenario = new EventScenario();
        _eventSourceId = EventSourceId.New();
        _consumerScope = Substitute.For<IConsumerScope>();
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();
        services.Configure<ArcOptions>(_ => { });
        services.AddCratisArcCore();
        services.AddSingleton(_scenario);
        services.AddSingleton(_consumerScope);
        _provider = services.BuildServiceProvider();
    }

    async Task Because() => _result = await _provider.GetRequiredService<ICommandPipeline>().Execute(new AppendConsumerEvent(_eventSourceId));

    [Fact] void should_discover_the_consumer_scope_in_the_outer_pipeline() => _provider.GetRequiredService<IInstancesOf<ICommandExecutionScope>>().Select(_ => _.GetType()).ShouldContain(typeof(ConsumerCommandScope));
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_fail_resolving_consumer_dependencies() => _result.ExceptionMessages.ShouldBeEmpty();
    [Fact] void should_begin_only_the_outer_command_scope() => _consumerScope.Received(1).Begin(Arg.Any<CommandContext>());
    [Fact] void should_complete_only_the_outer_command_scope() => _consumerScope.Received(1).Complete(Arg.Any<CommandContext>(), Arg.Is<CommandResult>(_ => _.IsSuccess));
    [Fact] async Task should_store_the_event_through_the_kernel_pipeline() => await _scenario.EventSequence.ShouldHaveAppendedEvent<TestEvent>(_eventSourceId, _ => _.Value == "from consumer");

    public void Dispose()
    {
        _provider.Dispose();
        _scenario.Dispose();
    }
}
