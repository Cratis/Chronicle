// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_ReducerInvoker.when_creating_for;

public class reducer_with_no_handler_methods : given.a_reducer_invoker_for<ReducerWithNoHandlerMethods>
{
    [Fact] void should_not_have_any_event_types() => _invoker.EventTypes.ShouldBeEmpty();
}
