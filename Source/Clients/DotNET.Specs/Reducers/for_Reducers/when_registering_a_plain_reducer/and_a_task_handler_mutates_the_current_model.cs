// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.for_EventStore.when_preparing_for_registration.given;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_registering_a_plain_reducer;

public class and_a_task_handler_mutates_the_current_model : an_explicit_store
{
    Model _current;
    ReduceResult _result;

    void Establish()
    {
        _current = new() { Name = "before" };
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>().RegisterReducer<MutationReducer>();
        _activator.Activate(Arg.Any<IServiceProvider>(), typeof(MutationReducer))
            .Returns(new ActivatedArtifact(new MutationReducer(), typeof(MutationReducer), NullLogger<ActivatedArtifact>.Instance));
        _store = CreateStore();
    }

    async Task Because()
    {
        await _store.DiscoverAll();
        _result = await _store.Reducers.GetHandlerFor<MutationReducer>().Invoker.Invoke(
            _provider,
            [new EventAndContext(new PlainEvent("after"), EventContext.Empty)],
            _current);
    }

    [Fact] void should_preserve_classic_task_mutation_semantics() => _current.Name.ShouldEqual("after");
    [Fact] void should_preserve_the_classic_null_result_for_plain_task_handlers() => _result.ReadModelState.ShouldBeNull();
    [Fact] void should_complete_successfully() => _result.IsSuccess.ShouldBeTrue();

    public class Model
    {
        public string Name { get; set; } = string.Empty;
    }

    public class MutationReducer
    {
        public Task Apply(PlainEvent @event, Model? current)
        {
            current!.Name = @event.Name;
            return Task.CompletedTask;
        }
    }
}
