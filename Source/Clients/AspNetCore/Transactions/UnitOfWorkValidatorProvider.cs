// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.AspNetCore.Transactions;

/// <summary>
/// Represents a <see cref="IModelValidatorProvider"/> for units of work.
/// </summary>
public class UnitOfWorkValidatorProvider : IModelValidatorProvider
{
    /// <inheritdoc/>
    public void CreateValidators(ModelValidatorProviderContext context)
    {
        var unitOfWorkManager = GlobalInstances.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
        context.Results.Add(new ValidatorItem
        {
            IsReusable = true,
            Validator = new UnitOfWorkValidator(unitOfWorkManager)
        });
    }
}
