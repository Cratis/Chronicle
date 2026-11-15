// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Reactors.Kernel;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Setup.Serialization.for_GeneratedGenericInvokablesTypeFilter;

public class when_formatting_a_subscribe_request_for_a_kernel_reactor : given.a_kernel_type_allow_list
{
    Exception _error;
    string _formatted;

    void Because() => _error = Catch.Exception(() =>
        _formatted = _converter.Format(_subscribeInvokable.MakeGenericType(typeof(IReactorObserverSubscriber<WebhookReactor>))));

    [Fact] void should_allow_the_request_type() => _error.ShouldBeNull();
    [Fact] void should_format_it() => _formatted.ShouldNotBeNull();
}
