// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Namespaces;

/// <summary>The namespace is closing and cannot admit an Ensure.</summary>
/// <param name="namespace">The canonical namespace name.</param>
public class NamespaceClosing(EventStoreNamespaceName @namespace) : Exception($"Namespace '{@namespace}' is closing.");
