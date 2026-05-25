// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Represents an implementation of <see cref="ITranslateBuilder"/>.
/// </summary>
public class TranslateBuilder : ITranslateBuilder
{
    readonly List<TranslateValue> _translations = [];

    /// <inheritdoc/>
    public ITranslateBuilder Map(string from, string to)
    {
        _translations.Add(new(from, to));

        return this;
    }

    /// <summary>
    /// Builds the configured translation entries.
    /// </summary>
    /// <returns>The configured translation entries.</returns>
    public IReadOnlyList<TranslateValue> Build() => _translations.ToArray();
}
