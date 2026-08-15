// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Attribute used to indicate that a property should be set to a constant value when an event of the specified type occurs.
/// </summary>
/// <typeparam name="TEvent">The type of event that triggers the value assignment.</typeparam>
/// <param name="value">
/// The constant value to set. Must be a compile-time constant such as a string, number, boolean, or enum value.
/// Pass <see langword="null"/> to clear the member back to no value - the same declaration as
/// <see cref="ClearWithAttribute{TEvent}"/> on the member, which reads better for that intent.
/// </param>
/// <remarks>
/// The parameter is nullable so a clear can be written as a plain <see langword="null"/>. A non-nullable parameter
/// forced every author of this declaration through the null-forgiving operator to get past the nullable analysis.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class SetValueAttribute<TEvent>(object? value) : Attribute, IProjectionAnnotation, ISetValueAttribute
{
    /// <inheritdoc/>
    public Type EventType => typeof(TEvent);

    /// <inheritdoc/>
    public object? Value { get; } = value;
}
