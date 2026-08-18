// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.InMemory.Sinks;

/// <summary>
/// Runs the shared <see cref="ISink"/> contract against <see cref="InMemorySink"/>.
/// </summary>
public class InMemorySinkHarness : ISinkHarness
{
    InMemorySink? _sink;

    /// <inheritdoc/>
    public ISink CreateSink(ReadModelDefinition definition) => _sink = new InMemorySink(definition, new TypeFormats());

    /// <inheritdoc/>
    public void Dispose() => _sink?.Dispose();
}
