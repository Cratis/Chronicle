// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Defines the builder for configuring value translations.
/// </summary>
public interface ITranslateBuilder
{
    /// <summary>
    /// Maps a source value to a target value.
    /// </summary>
    /// <param name="from">The source value.</param>
    /// <param name="to">The target value.</param>
    /// <returns>The builder continuation.</returns>
    ITranslateBuilder Map(string from, string to);
}
