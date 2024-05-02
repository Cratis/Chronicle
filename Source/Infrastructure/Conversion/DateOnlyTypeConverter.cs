// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Conversion;

/// <summary>
/// Represents a <see cref="StringTypeConverterBase{T}"/> for <see cref="DateOnly"/>.
/// </summary>
public class DateOnlyTypeConverter : StringTypeConverterBase<DateOnly>
{
    /// <inheritdoc/>
    public override DateOnly Parse(string source) => DateOnly.Parse(source);

    /// <inheritdoc/>
    public override string ToString(DateOnly source) => source.ToString("O");
}
