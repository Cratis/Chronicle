// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Transactions;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Cratis.Chronicle.AspNetCore.Transactions;

/// <summary>
/// Represents a <see cref="IModelValidator"/> for units of work.
/// </summary>
/// <param name="unitOfWorkManager">The <see cref="IUnitOfWorkManager"/> to use.</param>
public class UnitOfWorkValidator(IUnitOfWorkManager unitOfWorkManager) : IModelValidator
{
    /// <inheritdoc/>
    public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context)
    {
        if (unitOfWorkManager.HasCurrent)
        {
            var unitOfWork = unitOfWorkManager.Current;

            return unitOfWork.GetConstraintViolations().Select(violation =>
            {
                var memberName = violation.Details.TryGetValue(WellKnownConstraintDetailKeys.PropertyName, out var propertyName) ? propertyName : string.Empty;
                return new ModelValidationResult(memberName, violation.Message);
            }).ToArray();
        }

        return [];
    }
}
