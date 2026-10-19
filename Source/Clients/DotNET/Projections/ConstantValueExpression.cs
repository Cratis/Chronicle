// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Projections.Engine.Expressions;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Creates the literal expression shared by event-specific and every-event mappings.
/// </summary>
internal static class ConstantValueExpression
{
    /// <summary>
    /// Converts a constant to its invariant projection expression.
    /// </summary>
    /// <typeparam name="TProperty">Type of the constant.</typeparam>
    /// <param name="value">The constant value.</param>
    /// <returns>The constant or null expression.</returns>
    internal static IEventValueExpression From<TProperty>(TProperty value)
    {
        if (value is null)
        {
            return NullExpression.Instance;
        }

        object actualValue = value;
        if (actualValue.IsConcept())
        {
            actualValue = actualValue.GetConceptValue();
        }

        if (actualValue.GetType().IsEnum)
        {
            var underlyingType = Enum.GetUnderlyingType(actualValue.GetType());
            actualValue = underlyingType == typeof(int)
                ? Convert.ChangeType(actualValue, underlyingType)
                : actualValue.ToString()!;
        }

        var invariantString = actualValue switch
        {
            DateTime dateTime => dateTime.ToString("o", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("o", CultureInfo.InvariantCulture),
            DateOnly dateOnly => dateOnly.ToString("o", CultureInfo.InvariantCulture),
            TimeOnly timeOnly => timeOnly.ToString("o", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => actualValue.ToString()!
        };

        return new ValueExpression(invariantString);
    }
}
