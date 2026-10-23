// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Adapts the Task-based fixture hooks to the xUnit v3 lifetime contract.
/// </summary>
/// <typeparam name="TChronicleFixture">The type of the Chronicle fixture.</typeparam>
public abstract partial class ChronicleClientFixture<TChronicleFixture> : IAsyncLifetime
    where TChronicleFixture : IChronicleFixture
{
    ValueTask IAsyncLifetime.InitializeAsync() => new(InitializeAsync());

    ValueTask IAsyncDisposable.DisposeAsync() => new(DisposeAsync());
}
