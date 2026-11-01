// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventTypes.for_EventTypes.when_resolving_before_registration;

public class and_the_event_type_is_unknown : given.an_event_types_without_registration
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => _eventTypes.GetClrTypeFor("NotAKernelEvent"));

    [Fact] void should_throw_unknown_kernel_event_type() => _error.ShouldBeOfExactType<UnknownKernelEventType>();
}
