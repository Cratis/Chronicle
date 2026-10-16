// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Transactions.for_ReadProtectionReadRegion;

/// <summary>
/// Hosts built against earlier clients bind the suspension by reflection, exactly as below, until they move to
/// <see cref="IUnitOfWork.SuspendReadRegions"/>. Renaming it, making it public or giving it parameters breaks them.
/// </summary>
public class when_binding_suspend_by_reflection : Specification
{
    MethodInfo? _method;

    void Because() => _method = Type.GetType("Cratis.Chronicle.Transactions.ReadProtectionReadRegion, Cratis.Chronicle")?
        .GetMethod("Suspend", BindingFlags.Static | BindingFlags.NonPublic, Type.EmptyTypes);

    [Fact] void should_find_the_method() => _method.ShouldNotBeNull();
    [Fact] void should_return_a_disposable() => typeof(IDisposable).IsAssignableFrom(_method!.ReturnType).ShouldBeTrue();
}
