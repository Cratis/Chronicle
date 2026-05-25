// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Defines the builder for configuring map operations.
/// </summary>
public interface IMapBuilder
{
    /// <summary>
    /// Renames a source property into a target property.
    /// </summary>
    /// <param name="sourceProperty">The source property.</param>
    /// <param name="targetProperty">The target property.</param>
    /// <returns>The builder continuation.</returns>
    IMapBuilder Rename(string sourceProperty, string targetProperty);

    /// <summary>
    /// Assigns a template value to a target property.
    /// </summary>
    /// <param name="target">The target property.</param>
    /// <param name="template">The template.</param>
    /// <returns>The builder continuation.</returns>
    IMapBuilder Template(string target, string template);

    /// <summary>
    /// Adds a translation operation.
    /// </summary>
    /// <param name="target">The target property.</param>
    /// <param name="source">The source property.</param>
    /// <param name="entries">Callback for configuring translation entries.</param>
    /// <returns>The builder continuation.</returns>
    IMapBuilder Translate(string target, string source, Action<ITranslateBuilder> entries);

    /// <summary>
    /// Splits a source property into multiple target properties.
    /// </summary>
    /// <param name="source">The source property.</param>
    /// <param name="separator">The separator to split by.</param>
    /// <param name="targets">The target properties.</param>
    /// <returns>The builder continuation.</returns>
    IMapBuilder Split(string source, string separator, params string[] targets);
}
