// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Strings;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Aksio.Cratis.Applications.Validation;

/// <summary>
/// Extension methods for <see cref="ModelError"/>.
/// </summary>
public static class ModelErrorExtensions
{
    /// <summary>
    /// Convert a <see cref="ModelError"/> to <see cref="ValidationError"/> for a specific member.
    /// </summary>
    /// <param name="error"><see cref="ModelError"/> to convert.</param>
    /// <param name="member">Member the error is for.</param>
    /// <returns>A converted <see cref="ValidationError"/>.</returns>
    public static ValidationError ToValidationError(this ModelError error, string member)
    {
        member = string.Join('.', member.Split('.').Select(_ => _.ToCamelCase()));
        return new ValidationError(error.ErrorMessage, new string[] { member });
    }
}
