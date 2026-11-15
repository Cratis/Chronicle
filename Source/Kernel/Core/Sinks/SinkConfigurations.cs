// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Sinks;

/// <summary>
/// Represents an implementation of <see cref="ISinkConfigurations"/> backed by the kernel's <see cref="SinksOptions"/>.
/// </summary>
/// <param name="options">The <see cref="ChronicleOptions"/>.</param>
public class SinkConfigurations(IOptions<ChronicleOptions> options) : ISinkConfigurations
{
    /// <inheritdoc/>
    public ResolvedSink ResolveFor(ReadModelDefinition readModel, bool graph)
    {
        var sink = readModel.Sink;
        if (!SinkConfigurationRules.CanBeNamed(sink.Type))
        {
            return new(sink.Type, SinkName.Default);
        }

        var sinks = options.Value.Sinks;
        var named = sink.Name is { IsDefault: false } ? sink.Name : null;

        // The configured default applies only to a read model of the matching kind that names no sink: an explicit
        // sink type the default cannot serve - a graph type for read models, say - keeps its own implicit default.
        var name = named?.Value ?? (graph ? sinks.Graphs : sinks.ReadModels);
        if (string.IsNullOrWhiteSpace(name) || (named is null && !SinkConfigurationRules.Serves(Normalized(sink.Type), graph)))
        {
            return new(sink.Type, SinkName.Default);
        }

        if (!sinks.TryGet(name, out var key, out var configuration))
        {
            throw new UnknownSinkName(readModel.Identifier, name);
        }

        var type = SinkConfigurationRules.Normalize(configuration.Type) ?? new SinkTypeId(configuration.Type);
        if (!SinkConfigurationRules.Serves(type, graph))
        {
            throw new SinkConfigurationIsForAnotherKindOfReadModel(readModel.Identifier, key, type, graph);
        }

        return new(type, key);
    }

    static SinkTypeId Normalized(SinkTypeId type) => SinkConfigurationRules.Normalize(type.Value) ?? type;
}
