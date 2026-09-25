// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties;

/// <summary>
/// Represents a <see cref="IPropertyPathSegment"/> for a call to a well-known <see cref="DerivedPropertyFunction"/>
/// registered in <see cref="DerivedPropertyFunctions"/>, such as <c language="csharp">ISOWeek()</c>.
/// </summary>
/// <param name="Function">The <see cref="DerivedPropertyFunction"/> this segment calls.</param>
public record DerivedPropertyFunctionSegment(DerivedPropertyFunction Function) : IPropertyPathSegment
{
    /// <inheritdoc/>
    public string Value => Function.Name;

    /// <inheritdoc/>
    /// <remarks>
    /// Rendered as the bare function name, without a trailing <c language="csharp">()</c>. A rendered path is what
    /// travels to the kernel and is parsed back, and the registry is keyed by that bare name, so rendering it
    /// without parens keeps the round trip identical to the name it was resolved from. Parsing still accepts the
    /// parenthesized form, so a path written by hand as <c language="csharp">Occurred.Week()</c> resolves the same.
    /// </remarks>
    public override string ToString() => Value;
}
