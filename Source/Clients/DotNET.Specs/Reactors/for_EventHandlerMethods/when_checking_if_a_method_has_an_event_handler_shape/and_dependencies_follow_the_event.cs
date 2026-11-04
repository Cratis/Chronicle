// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_EventHandlerMethods.when_checking_if_a_method_has_an_event_handler_shape;

public class and_dependencies_follow_the_event : Specification
{
    static readonly Type[] _eventTypes = [typeof(TheEvent)];

    IDictionary<string, bool> _results;

    void Because() => _results = typeof(TheReactor)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
        .ToDictionary(method => method.Name, method => method.HasEventHandlerShape(_eventTypes));

    [Fact] void should_have_the_shape_with_only_the_event() => _results["OnlyEvent"].ShouldBeTrue();
    [Fact] void should_have_the_shape_with_an_event_context() => _results["WithEventContext"].ShouldBeTrue();
    [Fact] void should_have_the_shape_with_a_reactor_delivery() => _results["WithReactorDelivery"].ShouldBeTrue();
    [Fact] void should_have_the_shape_with_a_service() => _results["WithService"].ShouldBeTrue();
    [Fact] void should_have_the_shape_with_an_event_context_and_a_service() => _results["WithEventContextAndService"].ShouldBeTrue();
    [Fact] void should_have_the_shape_with_several_services() => _results["WithSeveralServices"].ShouldBeTrue();
    [Fact] void should_have_the_shape_with_a_read_model() => _results["WithReadModel"].ShouldBeTrue();

    public record TheEvent(string Something);

    public interface IService;

    public interface IOtherService;

    public record TheReadModel(string Something);

    public class TheReactor
    {
        public int OnlyEvent(TheEvent @event) => 0;
        public int WithEventContext(TheEvent @event, EventContext context) => 0;
        public int WithReactorDelivery(TheEvent @event, ReactorDelivery delivery) => 0;
        public int WithService(TheEvent @event, IService service) => 0;
        public int WithEventContextAndService(TheEvent @event, EventContext context, IService service) => 0;
        public int WithSeveralServices(TheEvent @event, IService service, IOtherService other) => 0;
        public int WithReadModel(TheEvent @event, TheReadModel readModel) => 0;
    }
}
