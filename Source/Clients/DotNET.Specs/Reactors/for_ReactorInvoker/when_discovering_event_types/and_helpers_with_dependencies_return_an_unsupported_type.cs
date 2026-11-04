// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_ReactorInvoker.when_discovering_event_types;

public class and_helpers_with_dependencies_return_an_unsupported_type : Specification
{
    Exception _error;
    IEnumerable<EventType> _result;

    void Because() => _error = Catch.Exception(() => _result = ReactorInvoker.GetEventTypesFor(new EventTypesForSpecifications([typeof(MyEvent)]), typeof(ReactorWithHelpers)));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_not_discover_any_event_types() => _result.ShouldBeEmpty();

    public interface IService;

    public record Unclaimed;

    public class ReactorWithHelpers
    {
        public static Unclaimed PublicStatic(MyEvent @event, IService service) => new();

        internal Unclaimed Internal(MyEvent @event, IService service) => new();

        protected Unclaimed Protected(MyEvent @event, IService service) => new();

        Unclaimed Private(MyEvent @event, IService service) => new();
    }
}
