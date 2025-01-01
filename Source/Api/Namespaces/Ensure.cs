// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Namespaces;

/// <summary>
/// Represents the command for ensuring a namespace.
/// </summary>
/// <param name="Namespace">Namespace to ensure.</param>
public record Ensure(string Namespace);
