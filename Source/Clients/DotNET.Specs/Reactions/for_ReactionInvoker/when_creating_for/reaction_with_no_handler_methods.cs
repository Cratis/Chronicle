// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactions.for_ObserverInvoker.when_creating_for;

public class reaction_with_no_handler_methods : given.an_reaction_invoker_for<ReactionWithNoHandlerMethods>
{
    [Fact] void should_not_have_any_event_types() => invoker.EventTypes.ShouldBeEmpty();
}
