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
    public override string ToString() => $"{Value}()";
}
