// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Specifications;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Represents a base class for specification by example type of context setups.
/// </summary>
/// <param name="fixture">The <see cref="ChronicleFixture"/>.</param>
public abstract class IntegrationSpecificationContext(ChronicleFixture fixture) : ChronicleOrleansFixture(fixture)
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

    Task OnEstablish() => InvokeMethod("Establish");

    Task OnBecause() => InvokeMethod("Because");

    Task OnDestroy() => InvokeMethod("Destroy");

    Task InvokeMethod(string name) =>
        (typeof(SpecificationMethods<,>).MakeGenericType(GetType(), typeof(Specification)).GetMethod(name, BindingFlags.Static | BindingFlags.Public)!.Invoke(null, [this]) as Task)!;
}

/// <summary>
/// Represents a base class for specification by example type of context setups.
/// </summary>
/// <typeparam name="TFactory">The web application factory type.</typeparam>
/// <typeparam name="TStartup">The startup class type.</typeparam>
/// <param name="fixture">The <see cref="ChronicleFixture"/>.</param>
#pragma warning disable SA1402
public abstract class IntegrationSpecificationContext<TFactory, TStartup>(ChronicleFixture fixture) : ChronicleClientFixture<TFactory, TStartup>(fixture)
#pragma warning restore SA1402
    where TFactory : ChronicleWebApplicationFactory<TStartup>
    where TStartup : class
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

    Task OnEstablish() => InvokeMethod("Establish");

    Task OnBecause() => InvokeMethod("Because");

    Task OnDestroy() => InvokeMethod("Destroy");

    Task InvokeMethod(string name) =>
        (typeof(SpecificationMethods<,>).MakeGenericType(GetType(), typeof(Specification)).GetMethod(name, BindingFlags.Static | BindingFlags.Public)!.Invoke(null, [this]) as Task)!;
}
