// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Represents a successfully resolved key.
/// </summary>
/// <param name="Key">The resolved key.</param>
/// <param name="JoinKey">Optional key to use when applying direct joins.</param>
public record ResolvedKey(Key Key, object? JoinKey = null) : KeyResolverResult;
