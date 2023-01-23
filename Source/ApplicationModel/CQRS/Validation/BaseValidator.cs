// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Aksio.Cratis.Concepts;
using FluentValidation;

namespace Aksio.Cratis.Applications.Validation;

/// <summary>
/// Represents a base validator that we use for discovery.
/// </summary>
/// <typeparam name="T">Type of object the validator is for.</typeparam>
public class BaseValidator<T> : AbstractValidator<T>
{
    /// <summary>
    /// Defines a validation rules for a property based on <see cref="ConceptAs{T}"/> for the actual concept type.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <typeparam name="TProperty">Type of the concept.</typeparam>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, TProperty> RuleForConcept<TProperty>(Expression<Func<T, TProperty>> expression) => RuleFor(expression);

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for string.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, string> RuleFor(Expression<Func<T, ConceptAs<string>>> expression) => Transform(expression, (value) => value?.Value ?? null!);

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for bool.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, bool> RuleFor(Expression<Func<T, ConceptAs<bool>>> expression) => Transform(expression, (value) => value.Value);

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for Guid.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, Guid> RuleFor(Expression<Func<T, ConceptAs<Guid>>> expression) => Transform(expression, (value) => value.Value);

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for DateOnly.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, DateOnly> RuleFor(Expression<Func<T, ConceptAs<DateOnly>>> expression) => Transform(expression, (value) => value.Value);

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for TimeOnly.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, TimeOnly> RuleFor(Expression<Func<T, ConceptAs<TimeOnly>>> expression) => Transform(expression, (value) => value.Value);

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for DateTime.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, DateTime> RuleFor(Expression<Func<T, ConceptAs<DateTime>>> expression) => Transform(expression, (value) => value.Value);

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for DateTimeOffset.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, DateTimeOffset> RuleFor(Expression<Func<T, ConceptAs<DateTimeOffset>>> expression) => Transform(expression, (value) => value.Value);

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for float.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, float> RuleFor(Expression<Func<T, ConceptAs<float>>> expression) => Transform(expression, (value) => value.Value);

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for double.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, double> RuleFor(Expression<Func<T, ConceptAs<double>>> expression) => Transform(expression, (value) => value.Value);

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for decimal.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, decimal> RuleFor(Expression<Func<T, ConceptAs<decimal>>> expression) => Transform(expression, (value) => value.Value);

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for sbyte.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, sbyte> RuleFor(Expression<Func<T, ConceptAs<sbyte>>> expression) => Transform(expression, (value) => value.Value);

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for short.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, short> RuleFor(Expression<Func<T, ConceptAs<short>>> expression) => Transform(expression, (value) => value.Value);

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for int.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, int> RuleFor(Expression<Func<T, ConceptAs<int>>> expression) => Transform(expression, (value) => value.Value);

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for long.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, long> RuleFor(Expression<Func<T, ConceptAs<long>>> expression) => Transform(expression, (value) => value.Value);

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for byte.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, byte> RuleFor(Expression<Func<T, ConceptAs<byte>>> expression) => Transform(expression, (value) => value.Value);

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for ushort.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, ushort> RuleFor(Expression<Func<T, ConceptAs<ushort>>> expression) => Transform(expression, (value) => value.Value);

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for uint.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, uint> RuleFor(Expression<Func<T, ConceptAs<uint>>> expression) => Transform(expression, (value) => value.Value);

    /// <summary>
    /// Defines a validation rules for a specific property based on <see cref="ConceptAs{T}"/> for ulong.
    /// </summary>
    /// <param name="expression">The expression representing the property to validate.</param>
    /// <returns>An IRuleBuilder instance on which validators can be defined.</returns>
    public IRuleBuilderInitial<T, ulong> RuleFor(Expression<Func<T, ConceptAs<ulong>>> expression) => Transform(expression, (value) => value.Value);
}
