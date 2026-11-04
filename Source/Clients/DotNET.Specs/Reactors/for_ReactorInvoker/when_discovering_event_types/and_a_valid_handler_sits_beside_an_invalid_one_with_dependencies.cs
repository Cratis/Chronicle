// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_ReactorInvoker.when_discovering_event_types;

public class and_a_valid_handler_sits_beside_an_invalid_one_with_dependencies : Specification
{
    Exception _result;

    void Because() => _result = Catch.Exception(() => ReactorInvoker.GetEventTypesFor(new EventTypesForSpecifications([typeof(MyEvent)]), typeof(MixedReactor)));

    [Fact] void should_fail_because_of_the_unsupported_return_type() => _result.ShouldBeOfExactType<InvalidReactorHandlerReturnType>();
    [Fact] void should_name_the_invalid_method() => _result.Message.ShouldContain("Invalid");

    public interface IService;

    public class MixedReactor
    {
        public Task Valid(MyEvent @event) => Task.CompletedTask;

        public int Invalid(MyEvent @event, IService service) => 0;
    }
}
