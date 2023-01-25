// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Aksio.Cratis.Objects;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Aksio.Cratis.Applications.ModelBinding;

/// <summary>
/// Represents a <see cref="IModelBinder"/> for <see cref="FromRequestBindingSource"/>.
/// </summary>
public class FromRequestModelBinder : IModelBinder
{
    static class DefaultValueChecker<T>
    {
        public static bool IsDefault(T value) => EqualityComparer<T>.Default.Equals(value, default);
    }

    static readonly Dictionary<Type, MethodInfo> _isDefaultMethodsByType = new();

    readonly IModelBinder _bodyModelBinder;
    readonly IModelBinder _complexModelBinder;

    /// <summary>
    /// Initializes a new instance of the <see cref="FromRequestModelBinder"/> class.
    /// </summary>
    /// <param name="bodyModelBinder">The <see cref="IModelBinder"/> for resolving values from the HTTP body.</param>
    /// <param name="complexModelBinder">The <see cref="IModelBinder"/> for resolving values from other parts of the HTTP request.</param>
    public FromRequestModelBinder(IModelBinder bodyModelBinder, IModelBinder complexModelBinder)
    {
        _bodyModelBinder = bodyModelBinder;
        _complexModelBinder = complexModelBinder;
    }

    /// <inheritdoc/>
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        await _bodyModelBinder.BindModelAsync(bindingContext);

        if (bindingContext.Result.IsModelSet)
        {
            var model = bindingContext.Result.Model!.Clone();
            await _complexModelBinder.BindModelAsync(bindingContext);
            var complexModel = bindingContext.Result.Model!.Clone();

            foreach (var property in model.GetType().GetProperties())
            {
                var modelValue = property.GetValue(model)!;
                var complexModelValue = property.GetValue(complexModel)!;
                var isModelDefault = IsDefaultValue(property.PropertyType, modelValue);
                var isComplexDefault = IsDefaultValue(property.PropertyType, complexModelValue);
                if (isModelDefault && !isComplexDefault)
                {
                    property.SetValue(model, complexModelValue);
                }
            }

            bindingContext.Result = ModelBindingResult.Success(model);
        }
        else
        {
            await _complexModelBinder.BindModelAsync(bindingContext);
        }
    }

    bool IsDefaultValue(Type type, object value)
    {
        MethodInfo isDefaultMethod;

        if (_isDefaultMethodsByType.ContainsKey(type))
        {
            isDefaultMethod = _isDefaultMethodsByType[type];
        }
        else
        {
            var equalityComparerType = typeof(DefaultValueChecker<>).MakeGenericType(type);
            isDefaultMethod = equalityComparerType.GetMethod(nameof(DefaultValueChecker<object>.IsDefault))!;
            _isDefaultMethodsByType[type] = isDefaultMethod;
        }

        return (bool)isDefaultMethod.Invoke(null, new[] { value })!;
    }
}
