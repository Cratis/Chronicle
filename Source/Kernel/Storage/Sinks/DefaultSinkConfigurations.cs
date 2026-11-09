// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Represents an <see cref="ISinkConfigurations"/> with no named configurations: every read model stores to its implicit default sink.
/// </summary>
public class DefaultSinkConfigurations : ISinkConfigurations
{
    /// <summary>
    /// Gets the shared instance.
    /// </summary>
    public static readonly DefaultSinkConfigurations Instance = new();

    /// <inheritdoc/>
    public ResolvedSink ResolveFor(ReadModelDefinition readModel, bool graph)
    {
        var name = readModel.Sink.Name;
        if (name?.IsDefault == false && SinkConfigurationRules.CanBeNamed(readModel.Sink.Type))
        {
            throw new UnknownSinkName(readModel.Identifier, name);
        }

        return new(readModel.Sink.Type, SinkName.Default);
    }
}
