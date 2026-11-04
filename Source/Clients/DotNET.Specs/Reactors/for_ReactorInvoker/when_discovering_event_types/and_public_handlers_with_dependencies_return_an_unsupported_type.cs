// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_ReactorInvoker.when_discovering_event_types;

public class and_public_handlers_with_dependencies_return_an_unsupported_type : Specification
{
    IEventTypes _eventTypes;
    IDictionary<string, Exception> _results;

    void Establish() => _eventTypes = new EventTypesForSpecifications([typeof(MyEvent)]);

    void Because() => _results = new Dictionary<string, Exception>
    {
        [nameof(WithService)] = Catch.Exception(() => ReactorInvoker.GetEventTypesFor(_eventTypes, typeof(WithService))),
        [nameof(WithEventContextAndService)] = Catch.Exception(() => ReactorInvoker.GetEventTypesFor(_eventTypes, typeof(WithEventContextAndService))),
        [nameof(WithSeveralServices)] = Catch.Exception(() => ReactorInvoker.GetEventTypesFor(_eventTypes, typeof(WithSeveralServices))),
        [nameof(WithReadModel)] = Catch.Exception(() => ReactorInvoker.GetEventTypesFor(_eventTypes, typeof(WithReadModel)))
    };

    [Fact] void should_fail_with_a_service() => _results[nameof(WithService)].ShouldBeOfExactType<InvalidReactorHandlerReturnType>();
    [Fact] void should_fail_with_an_event_context_and_a_service() => _results[nameof(WithEventContextAndService)].ShouldBeOfExactType<InvalidReactorHandlerReturnType>();
    [Fact] void should_fail_with_several_services() => _results[nameof(WithSeveralServices)].ShouldBeOfExactType<InvalidReactorHandlerReturnType>();
    [Fact] void should_fail_with_a_read_model() => _results[nameof(WithReadModel)].ShouldBeOfExactType<InvalidReactorHandlerReturnType>();
    [Fact] void should_name_the_reactor() => _results[nameof(WithService)].Message.ShouldContain(typeof(WithService).FullName);
    [Fact] void should_name_the_method() => _results[nameof(WithService)].Message.ShouldContain("Handle");
    [Fact] void should_name_the_return_type() => _results[nameof(WithService)].Message.ShouldContain(typeof(int).FullName);

    public interface IService;

    public interface IOtherService;

    public record TheReadModel(string Something);

    public class WithService
    {
        public int Handle(MyEvent @event, IService service) => 0;
    }

    public class WithEventContextAndService
    {
        public int Handle(MyEvent @event, EventContext context, IService service) => 0;
    }

    public class WithSeveralServices
    {
        public int Handle(MyEvent @event, IService service, IOtherService other) => 0;
    }

    public class WithReadModel
    {
        public int Handle(MyEvent @event, TheReadModel readModel) => 0;
    }
}
