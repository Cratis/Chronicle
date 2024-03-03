// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NJsonSchema;

namespace Cratis.Kernel.Projections.Expressions.EventValues;

/// <summary>
/// Exception that gets thrown when an event value expression is not supported.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="UnsupportedModelPropertyExpression"/> class.
/// </remarks>
/// <param name="property">The property that has unsupported expression.</param>
/// <param name="expression">The unsupported expression.</param>
public class UnsupportedEventValueExpression(JsonSchemaProperty property, string expression) : Exception($"Unknown event value expression '{expression}' for property '{property.Name}'")
{
}
