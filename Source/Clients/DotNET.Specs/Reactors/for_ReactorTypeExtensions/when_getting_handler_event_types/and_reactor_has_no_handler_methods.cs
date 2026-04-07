// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_ReactorTypeExtensions.when_getting_handler_event_types;

public class and_reactor_has_no_handler_methods : Specification
{
    class ReactorWithNoHandlerMethods : IReactor;

    IEnumerable<Type> _result;

    void Because() => _result = typeof(ReactorWithNoHandlerMethods).GetHandlerEventTypes();

    [Fact] void should_return_empty_collection() => _result.ShouldBeEmpty();
}
