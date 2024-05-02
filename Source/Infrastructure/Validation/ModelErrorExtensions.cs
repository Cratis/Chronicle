// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Strings;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Cratis.Validation;

/// <summary>
/// Extension methods for <see cref="ModelError"/>.
/// </summary>
public static class ModelErrorExtensions
{
    /// <summary>
    /// Convert a <see cref="ModelError"/> to <see cref="ValidationResult"/> for a specific member.
    /// </summary>
    /// <param name="error"><see cref="ModelError"/> to convert.</param>
    /// <param name="member">Member the error is for.</param>
    /// <returns>A converted <see cref="ValidationResult"/>.</returns>
    public static ValidationResult ToValidationResult(this ModelError error, string member)
    {
        member = string.Join('.', member.Split('.').Select(_ => _.ToCamelCase()));
        return new ValidationResult(ValidationResultSeverity.Error, error.ErrorMessage, new string[] { member }, new object());
    }
}
