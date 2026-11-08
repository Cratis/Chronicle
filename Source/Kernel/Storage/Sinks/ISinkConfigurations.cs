// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Defines the named sink configurations of the kernel and resolves which one a read model stores to.
/// </summary>
public interface ISinkConfigurations
{
    /// <summary>
    /// Resolve the sink a read model stores to.
    /// </summary>
    /// <param name="readModel">The <see cref="ReadModelDefinition"/> to resolve for.</param>
    /// <param name="graph">Whether the read model is a graph.</param>
    /// <returns>The <see cref="ResolvedSink"/>.</returns>
    /// <exception cref="UnknownSinkName">Thrown when the read model names a sink that is not configured.</exception>
    /// <exception cref="SinkConfigurationIsForAnotherKindOfReadModel">Thrown when the named sink cannot store this kind of read model.</exception>
    ResolvedSink ResolveFor(ReadModelDefinition readModel, bool graph);
}
