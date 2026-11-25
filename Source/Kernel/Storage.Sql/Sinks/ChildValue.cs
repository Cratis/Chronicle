// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Describes a child to upsert into a collection.
/// </summary>
/// <param name="IdentifierName">The name of the property identifying a child.</param>
/// <param name="IdentifierValue">The identifier value of the child.</param>
/// <param name="NewChild">The child to upsert.</param>
/// <param name="Ordering">The optional <see cref="ChildrenOrdering"/> placing a new child.</param>
/// <param name="ChildSchema">The optional schema of a child, giving the types its values are compared as when placing it.</param>
internal readonly record struct ChildValue(PropertyPath IdentifierName, object? IdentifierValue, object? NewChild, ChildrenOrdering? Ordering = default, JsonSchema? ChildSchema = default);
