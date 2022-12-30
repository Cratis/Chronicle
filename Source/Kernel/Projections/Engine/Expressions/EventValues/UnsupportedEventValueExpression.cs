// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NJsonSchema;

namespace Aksio.Cratis.Events.Projections.Expressions.EventValues;

/// <summary>
/// Exception that gets thrown when an event value expression is not supported.
/// </summary>
public class UnsupportedEventValueExpression : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedModelPropertyExpression"/> class.
    /// </summary>
    /// <param name="property">The property that has unsupported expression.</param>
    /// <param name="expression">The unsupported expression.</param>
    public UnsupportedEventValueExpression(JsonSchemaProperty property, string expression) : base($"Unknown event value expression '{expression}' for property '{property.Name}'")
    {
    }
}
