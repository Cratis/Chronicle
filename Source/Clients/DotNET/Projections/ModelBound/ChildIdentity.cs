// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// How the items of a children collection are identified.
/// </summary>
/// <param name="Key">The event property (or well-known expression) that carries the child key.</param>
/// <param name="KeyExpression">The key with the naming policy applied.</param>
/// <param name="IdentifiedBy">The child property that holds the identity, before the naming policy is applied.</param>
internal sealed record ChildIdentity(string Key, string KeyExpression, string? IdentifiedBy);
