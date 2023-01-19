// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Reflection;
using Aksio.Cratis.Types;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Aksio.Cratis.Applications.Validation;

/// <summary>
/// Represents a <see cref="IModelValidatorProvider"/> for <see cref="DiscoverableValidator{T}"/>.
/// </summary>
public class DiscoverableModelValidatorProvider : IModelValidatorProvider
{
    readonly IServiceProvider _serviceProvider;
    readonly IDictionary<Type, Type> _validatorTypesByModelType;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoverableModelValidatorProvider"/> class.
    /// </summary>
    /// <param name="types"><see cref="ITypes"/> for type discovery.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting instances of the validators.</param>
    public DiscoverableModelValidatorProvider(ITypes types, IServiceProvider serviceProvider)
    {
        var candidates = types.FindMultiple(typeof(IDiscoverableValidator<>));
        var invalidValidators = candidates.Where(_ =>
        {
            var interfaces = _.GetInterfaces();
            var validatorType = interfaces.Single(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IDiscoverableValidator<>));
            var modelType = validatorType.GetGenericArguments()[0];
            return !_.IsAssignableTo(typeof(AbstractValidator<>).MakeGenericType(modelType));
        }).ToArray();

        if (invalidValidators.Length > 0)
        {
            throw new DiscoverableValidatorMustImplementAbstractValidator(invalidValidators[0]);
        }

        _validatorTypesByModelType = candidates
            .ToDictionary(
                _ =>
                {
                    var current = _.BaseType!;
                    while (!current.IsDerivedFromOpenGeneric(typeof(AbstractValidator<>)))
                    {
                        current = current.BaseType!;
                    }
                    return current.GetGenericArguments()[0];
                },
                _ => _);
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public void CreateValidators(ModelValidatorProviderContext context)
    {
        if (_validatorTypesByModelType.ContainsKey(context.ModelMetadata.ModelType))
        {
            var validator = (_serviceProvider.GetRequiredService(_validatorTypesByModelType[context.ModelMetadata.ModelType]) as IValidator)!;
            var modelValidator = new DiscoverableModelValidator(validator);
            context.Results.Add(new ValidatorItem
            {
                IsReusable = false,
                Validator = modelValidator
            });
        }
    }
}
