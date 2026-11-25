// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Diagnostics.Load.for_KernelLoad;

public class when_inspecting_the_lifetime : Specification
{
    /// <summary>Every instance registers its own gauges on the shared meter, so a transient leaks five instruments per resolution.</summary>
    [Fact] void should_be_registered_as_a_singleton() => typeof(KernelLoad).GetCustomAttributes(typeof(SingletonAttribute), false).ShouldNotBeEmpty();
}
