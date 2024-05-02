// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.API.Namespaces;

/// <summary>
/// Represents a namespace.
/// </summary>
/// <param name="Name">The name of the namespace.</param>
/// <param name="Description">The description of the namespace.</param>
public record Namespace(string Name, string Description);
