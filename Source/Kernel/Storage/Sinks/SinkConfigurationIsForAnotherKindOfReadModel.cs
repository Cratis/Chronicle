// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// The exception that is thrown when a read model names a sink whose type cannot store that kind of read model.
/// </summary>
/// <param name="readModel">The <see cref="ReadModelIdentifier"/> of the read model.</param>
/// <param name="name">The <see cref="SinkName"/> it names.</param>
/// <param name="type">The <see cref="SinkTypeId"/> of the named configuration.</param>
/// <param name="graph">Whether the read model is a graph.</param>
public class SinkConfigurationIsForAnotherKindOfReadModel(ReadModelIdentifier readModel, SinkName name, SinkTypeId type, bool graph)
    : Exception(graph
        ? $"Graph '{readModel}' stores to sink '{name}', whose type '{type}' cannot store graphs."
        : $"Read model '{readModel}' stores to sink '{name}', whose type '{type}' only stores graphs.");
