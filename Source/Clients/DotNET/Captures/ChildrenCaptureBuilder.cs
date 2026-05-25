// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Represents an implementation of <see cref="IChildrenCaptureBuilder"/>.
/// </summary>
public class ChildrenCaptureBuilder : IChildrenCaptureBuilder
{
    readonly List<AppendDefinition> _appends = [];
    MapDefinition? _map;

    /// <inheritdoc/>
    public IChildrenCaptureBuilder Map(Action<IMapBuilder> configure)
    {
        var builder = new MapBuilder();
        configure(builder);
        _map = builder.Build();

        return this;
    }

    /// <inheritdoc/>
    public IChildrenCaptureBuilder Append<TEvent>(Action<IAppendBuilder<TEvent>> configure)
        where TEvent : class
    {
        var builder = new AppendBuilder<TEvent>();
        configure(builder);
        _appends.Add(builder.Build());

        return this;
    }

    /// <summary>
    /// Builds the <see cref="ChildrenDefinition"/>.
    /// </summary>
    /// <param name="collectionProperty">The child collection path.</param>
    /// <param name="identifiedBy">The property identifying a child item.</param>
    /// <returns>A new <see cref="ChildrenDefinition"/>.</returns>
    public ChildrenDefinition Build(string collectionProperty, string identifiedBy) => new(collectionProperty, identifiedBy, _map, _appends.ToArray());
}
