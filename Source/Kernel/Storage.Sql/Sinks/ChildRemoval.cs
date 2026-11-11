// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Describes the removal of a child from every collection found at a path.
/// </summary>
/// <param name="Segments">The segments of the path to the children collection.</param>
/// <param name="Identifier">The name of the property identifying a child.</param>
/// <param name="Key">The identifier value of the child to remove.</param>
internal sealed record ChildRemoval(IPropertyPathSegment[] Segments, PropertyPath Identifier, object? Key);
