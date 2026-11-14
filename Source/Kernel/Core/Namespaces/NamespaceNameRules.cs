// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Namespaces;

/// <summary>
/// Represents the rules a namespace name must follow.
/// </summary>
public static class NamespaceNameRules
{
    /// <summary>
    /// The message used when a namespace name contains a comma.
    /// </summary>
    public const string CommaMessage = "The namespace name must not contain a comma. A name like 'a,a' usually means the tenant header was sent twice.";

    /// <summary>
    /// Gets whether a namespace name contains a comma.
    /// </summary>
    /// <param name="name">The namespace name.</param>
    /// <returns>True if it contains a comma; false otherwise.</returns>
    public static bool ContainsComma(string name) => name.Contains(',');
}
