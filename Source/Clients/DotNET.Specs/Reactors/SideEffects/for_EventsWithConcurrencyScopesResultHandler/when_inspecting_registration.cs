// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Reactors.SideEffects.for_EventsWithConcurrencyScopesResultHandler;

public class when_inspecting_registration : Specification
{
    [Fact] void should_be_discovered_as_a_singleton() =>
        Attribute.IsDefined(typeof(EventsWithConcurrencyScopesResultHandler), typeof(SingletonAttribute)).ShouldBeTrue();
}
