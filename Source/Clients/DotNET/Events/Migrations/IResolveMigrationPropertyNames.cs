// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Resolves typed migration accessors without reinterpreting raw JSON paths.
/// </summary>
internal interface IResolveMigrationPropertyNames
{
    /// <summary>
    /// Resolves an accessor to its serialized property path.
    /// </summary>
    /// <param name="expression">The typed member accessor.</param>
    /// <returns>The JSON property path.</returns>
    PropertyName ResolvePropertyName(LambdaExpression expression);
}
