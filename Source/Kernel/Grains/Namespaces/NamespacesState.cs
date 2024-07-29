// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Grains.Namespaces;

/// <summary>
/// Represents the state of namespaces.
/// </summary>
public class NamespacesState
{
    /// <summary>
    /// Gets the namespaces.
    /// </summary>
    public IList<NamespaceState> Namespaces { get; init; } = [];

    /// <summary>
    /// Gets the new namespaces.
    /// </summary>
    public IList<NamespaceState> NewNamespaces { get; init; } = [];
}
