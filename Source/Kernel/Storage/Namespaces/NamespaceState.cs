// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Namespaces;

/// <summary>
/// Represents the state of a namespace.
/// </summary>
/// <param name="Name"><see cref="EventStoreNamespaceName"/> for the namespace. </param>
/// <param name="Created">WHen it was created.</param>
public record NamespaceState(EventStoreNamespaceName Name, DateTimeOffset Created);
