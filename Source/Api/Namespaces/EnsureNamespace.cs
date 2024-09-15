// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Api.Namespaces;

/// <summary>
/// Represents the command for adding a namespace.
/// </summary>
/// <param name="Name">Name of the namespace to add.</param>
public record EnsureNamespace(EventStoreNamespaceName Name);
