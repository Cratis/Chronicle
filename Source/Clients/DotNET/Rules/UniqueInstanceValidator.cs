// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using FluentValidation;
using FluentValidation.Validators;

namespace Cratis.Chronicle.Rules;

/// <summary>
/// Represents a <see cref="PropertyValidator{T, TProperty}"/> for checking for uniqueness.
/// </summary>
/// <typeparam name="T">Type of object.</typeparam>
/// <typeparam name="TProperty">Type of property.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="UniqueInstanceValidator{T, TProperty}"/> class.
/// </remarks>
/// <param name="getValue">Func for getting the value to check for uniqueness with.</param>
public class UniqueInstanceValidator<T, TProperty>(Func<object, object> getValue) : PropertyValidator<T, TProperty>
    where TProperty : IEnumerable
{
    /// <inheritdoc/>
    public override string Name => nameof(UniqueInstanceValidator<T, TProperty>);

    /// <inheritdoc/>
    public override bool IsValid(ValidationContext<T> context, TProperty value)
    {
        var parent = (context as IValidationContext)!.ParentContext;

        var valueToCheck = getValue(parent.InstanceToValidate);
        foreach (var element in value)
        {
            if (element.Equals(valueToCheck))
            {
                return false;
            }
        }

        return true;
    }
}
