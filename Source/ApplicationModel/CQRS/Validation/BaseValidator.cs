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
    public IRuleBuilderInitial<T, string> RuleFor(Expression<Func<T, ConceptAs<string>>> expression) => Transform(expression, (value) => value?.Value ?? null!);
    public IRuleBuilderInitial<T, bool> RuleFor(Expression<Func<T, ConceptAs<bool>>> expression) => Transform(expression, (value) => value.Value);
    public IRuleBuilderInitial<T, Guid> RuleFor(Expression<Func<T, ConceptAs<Guid>>> expression) => Transform(expression, (value) => value.Value);
    public IRuleBuilderInitial<T, DateOnly> RuleFor(Expression<Func<T, ConceptAs<DateOnly>>> expression) => Transform(expression, (value) => value.Value);
    public IRuleBuilderInitial<T, TimeOnly> RuleFor(Expression<Func<T, ConceptAs<TimeOnly>>> expression) => Transform(expression, (value) => value.Value);
    public IRuleBuilderInitial<T, DateTime> RuleFor(Expression<Func<T, ConceptAs<DateTime>>> expression) => Transform(expression, (value) => value.Value);
    public IRuleBuilderInitial<T, DateTimeOffset> RuleFor(Expression<Func<T, ConceptAs<DateTimeOffset>>> expression) => Transform(expression, (value) => value.Value);
    public IRuleBuilderInitial<T, float> RuleFor(Expression<Func<T, ConceptAs<float>>> expression) => Transform(expression, (value) => value.Value);
    public IRuleBuilderInitial<T, double> RuleFor(Expression<Func<T, ConceptAs<double>>> expression) => Transform(expression, (value) => value.Value);
    public IRuleBuilderInitial<T, decimal> RuleFor(Expression<Func<T, ConceptAs<decimal>>> expression) => Transform(expression, (value) => value.Value);
    public IRuleBuilderInitial<T, sbyte> RuleFor(Expression<Func<T, ConceptAs<sbyte>>> expression) => Transform(expression, (value) => value.Value);
    public IRuleBuilderInitial<T, short> RuleFor(Expression<Func<T, ConceptAs<short>>> expression) => Transform(expression, (value) => value.Value);
    public IRuleBuilderInitial<T, int> RuleFor(Expression<Func<T, ConceptAs<int>>> expression) => Transform(expression, (value) => value.Value);
    public IRuleBuilderInitial<T, long> RuleFor(Expression<Func<T, ConceptAs<long>>> expression) => Transform(expression, (value) => value.Value);
    public IRuleBuilderInitial<T, byte> RuleFor(Expression<Func<T, ConceptAs<byte>>> expression) => Transform(expression, (value) => value.Value);
    public IRuleBuilderInitial<T, ushort> RuleFor(Expression<Func<T, ConceptAs<ushort>>> expression) => Transform(expression, (value) => value.Value);
    public IRuleBuilderInitial<T, uint> RuleFor(Expression<Func<T, ConceptAs<uint>>> expression) => Transform(expression, (value) => value.Value);
    public IRuleBuilderInitial<T, ulong> RuleFor(Expression<Func<T, ConceptAs<ulong>>> expression) => Transform(expression, (value) => value.Value);
}
