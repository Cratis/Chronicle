// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Attribute used to indicate that a property should be set to a constant value when an event of the specified type occurs.
/// </summary>
/// <typeparam name="TEvent">The type of event that triggers the value assignment.</typeparam>
/// <param name="value">The constant value to set. Must be a compile-time constant such as a string, number, boolean, or enum value.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class SetValueAttribute<TEvent>(object value) : Attribute, IProjectionAnnotation, ISetValueAttribute
{
    /// <inheritdoc/>
    public Type EventType => typeof(TEvent);

    /// <inheritdoc/>
    public object Value { get; } = value;
}
