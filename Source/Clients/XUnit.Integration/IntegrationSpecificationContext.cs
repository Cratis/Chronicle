// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Specifications;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Represents a base class for specification by example type of context setups.
/// </summary>
/// <param name="fixture">The <see cref="ChronicleFixture"/>.</param>
public abstract class IntegrationSpecificationContext(ChronicleFixture fixture) : OrleansFixture(fixture)
{
    /// <inheritdoc/>
    protected override async Task OnInitializeAsync()
    {
        EnsureBuilt();
        await OnEstablish();
        await OnBecause();
    }

    /// <inheritdoc/>
    protected override Task OnDisposeAsync() => OnDestroy();

    Task OnEstablish()
    {
        return InvokeMethod("Establish");
    }

    Task OnBecause()
    {
        return InvokeMethod("Because");
    }

    Task OnDestroy()
    {
        return InvokeMethod("Destroy");
    }

    Task InvokeMethod(string name)
    {
#nullable disable
        return typeof(SpecificationMethods<,>).MakeGenericType(GetType(), typeof(Specification)).GetMethod(name, BindingFlags.Static | BindingFlags.Public).Invoke(null, [this]) as Task;
    }
}
