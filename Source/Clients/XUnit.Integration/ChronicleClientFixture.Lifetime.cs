// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Provides the xUnit v2 fixture lifetime contract.
/// </summary>
/// <typeparam name="TChronicleFixture">The type of the Chronicle fixture.</typeparam>
public abstract partial class ChronicleClientFixture<TChronicleFixture> : IAsyncLifetime
    where TChronicleFixture : IChronicleFixture;
