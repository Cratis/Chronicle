// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Reactors.Kernel;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Setup.Serialization.for_GeneratedGenericInvokablesTypeFilter;

public class when_asked_whether_types_are_allowed : given.a_kernel_type_allow_list
{
    bool? _openGeneric;
    bool? _closedGeneric;
    bool? _closedInvokable;
    bool? _subscribeInvokableDefinition;

    void Because()
    {
        _openGeneric = _filter.IsTypeAllowed(typeof(List<>));
        _closedGeneric = _filter.IsTypeAllowed(typeof(List<string>));
        _closedInvokable = _filter.IsTypeAllowed(_subscribeInvokable.MakeGenericType(typeof(IReactorObserverSubscriber<WebhookReactor>)));
        _subscribeInvokableDefinition = _filter.IsTypeAllowed(_subscribeInvokable);
    }

    [Fact] void should_not_decide_for_an_unregistered_generic_definition() => _openGeneric.ShouldBeNull();
    [Fact] void should_not_decide_for_a_closed_generic_type() => _closedGeneric.ShouldBeNull();
    [Fact] void should_leave_the_generic_arguments_of_a_closed_invokable_to_the_other_filters() => _closedInvokable.ShouldBeNull();
    [Fact] void should_allow_the_generated_invokable_definition() => _subscribeInvokableDefinition.ShouldEqual(true);
}
