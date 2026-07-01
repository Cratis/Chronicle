// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Represents an implementation of <see cref="IMapBuilder"/>.
/// </summary>
public class MapBuilder : IMapBuilder
{
    readonly List<MapOperation> _operations = [];

    /// <inheritdoc/>
    public IMapBuilder Rename(string sourceProperty, string targetProperty)
    {
        _operations.Add(new FieldRenameOperation(sourceProperty, targetProperty));

        return this;
    }

    /// <inheritdoc/>
    public IMapBuilder Template(string target, string template)
    {
        _operations.Add(new TemplateAssignOperation(target, template));

        return this;
    }

    /// <inheritdoc/>
    public IMapBuilder Translate(string target, string source, Action<ITranslateBuilder> entries)
    {
        var builder = new TranslateBuilder();
        entries(builder);
        _operations.Add(new TranslateOperation(target, source, builder.Build()));

        return this;
    }

    /// <inheritdoc/>
    public IMapBuilder Split(string source, string separator, params string[] targets)
    {
        _operations.Add(new SplitOperation(source, separator, targets));

        return this;
    }

    /// <summary>
    /// Builds the <see cref="MapDefinition"/>.
    /// </summary>
    /// <returns>A new <see cref="MapDefinition"/>.</returns>
    public MapDefinition Build() => new(_operations.ToArray());
}
