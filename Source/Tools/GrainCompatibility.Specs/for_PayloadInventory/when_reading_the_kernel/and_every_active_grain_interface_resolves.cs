// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.when_reading_the_kernel;

public class and_every_active_grain_interface_resolves : given.the_kernel_inventory
{
    [Fact] void should_have_roots() => _interfaces.ShouldNotBeEmpty();
    [Fact] void should_resolve_every_root() => _interfaces.Length.ShouldEqual(_grainContracts.ActiveInterfaces.Count());
}
