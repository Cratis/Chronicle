// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Defines a system for working with the type format metadata.
/// </summary>
public interface ITypeFormats
{
    /// <summary>
    /// Check if a type has a known format.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns>True if it is, false if not.</returns>
    bool IsKnown(Type type);

    /// <summary>
    /// Check if a format has a known type.
    /// </summary>
    /// <param name="format">Format to check.</param>
    /// <returns>True if it is, false if not.</returns>
    bool IsKnown(string format);

    /// <summary>
    /// Get the CLR type represented by a format.
    /// </summary>
    /// <param name="format">Known format.</param>
    /// <returns>The type.</returns>
    Type GetTypeForFormat(string format);

    /// <summary>
    /// Get the known format metadata for a CLR type.
    /// </summary>
    /// <param name="type">CLR type.</param>
    /// <returns>Known format.</returns>
    string GetFormatForType(Type type);
}
