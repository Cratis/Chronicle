// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Numerics;
using Cratis.Chronicle.Projections.Engine.Expressions;
using Cratis.Chronicle.Properties;

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
    /// <param name="targetProperty">The property receiving the constant.</param>
    /// <returns>The constant or null expression.</returns>
    /// <exception cref="UnsupportedProjectionConstant">The value is not a supported scalar constant.</exception>
    internal static IEventValueExpression From<TProperty>(TProperty value, PropertyPath targetProperty)
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

        if (!IsSupported(actualValue))
        {
            throw new UnsupportedProjectionConstant(targetProperty, actualValue.GetType());
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

    static bool IsSupported(object value) =>
        value.GetType().IsPrimitive || value.GetType().IsEnum ||
        value is string or decimal or Guid or DateTime or DateTimeOffset or DateOnly or TimeOnly or TimeSpan or Uri or
            Half or Int128 or UInt128 or BigInteger;
}
