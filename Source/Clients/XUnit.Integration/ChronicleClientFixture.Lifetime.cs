// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Cratis.Chronicle.XUnit.Integration;

public abstract partial class ChronicleClientFixture<TChronicleFixture> : IAsyncLifetime
    where TChronicleFixture : IChronicleFixture;
