// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents a successfully resolved key.
/// </summary>
/// <param name="Key">The resolved key.</param>
public record ResolvedKey(Key Key) : KeyResolverResult;
