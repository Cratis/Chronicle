// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Expressions.Keys;

/// <summary>
/// The exception that is thrown when a composite key expression contains a malformed component.
/// </summary>
/// <param name="expression">The complete expression.</param>
/// <param name="component">The malformed component.</param>
public class InvalidCompositeKeyExpression(string expression, string component) : Exception($"Invalid composite key expression '{expression}': component '{component}' must be a type name followed by property=expression mappings, or property=expression mappings alone")
{
    /// <summary>
    /// Gets the malformed component.
    /// </summary>
    public string Component { get; } = component;
}
