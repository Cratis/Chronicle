// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Represents an implementation of <see cref="INestedCaptureBuilder"/>.
/// </summary>
public class NestedCaptureBuilder : INestedCaptureBuilder
{
    readonly List<AppendDefinition> _appends = [];
    MapDefinition? _map;

    /// <inheritdoc/>
    public INestedCaptureBuilder Map(Action<IMapBuilder> configure)
    {
        var builder = new MapBuilder();
        configure(builder);
        _map = builder.Build();

        return this;
    }

    /// <inheritdoc/>
    public INestedCaptureBuilder Append<TEvent>(Action<IAppendBuilder<TEvent>> configure)
        where TEvent : class
    {
        var builder = new AppendBuilder<TEvent>();
        configure(builder);
        _appends.Add(builder.Build());

        return this;
    }

    /// <summary>
    /// Builds the <see cref="NestedDefinition"/>.
    /// </summary>
    /// <param name="objectPath">The nested object path.</param>
    /// <returns>A new <see cref="NestedDefinition"/>.</returns>
    public NestedDefinition Build(string objectPath) => new(objectPath, _map, _appends.ToArray());
}
