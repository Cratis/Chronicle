// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Aksio.Cratis.Applications.ModelBinding;

/// <summary>
/// Represents a <see cref="IModelBinderProvider"/> supporting the <see cref="FromRequestBindingSource"/>.
/// </summary>
public class FromRequestModelBinderProvider : IModelBinderProvider
{
    readonly BodyModelBinderProvider _bodyModelBinderProvider;
    readonly ComplexObjectModelBinderProvider _complexObjectModelBinderProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="FromRequestModelBinderProvider"/> class.
    /// </summary>
    /// <param name="bodyModelBinderProvider">The <see cref="BodyModelBinderProvider"/>.</param>
    /// <param name="complexObjectModelBinderProvider">The <see cref="ComplexObjectModelBinderProvider"/>.</param>
    public FromRequestModelBinderProvider(
        BodyModelBinderProvider bodyModelBinderProvider,
        ComplexObjectModelBinderProvider complexObjectModelBinderProvider)
    {
        _bodyModelBinderProvider = bodyModelBinderProvider;
        _complexObjectModelBinderProvider = complexObjectModelBinderProvider;
    }

    /// <inheritdoc/>
    public IModelBinder? GetBinder(ModelBinderProviderContext context) =>
        context.BindingInfo.BindingSource switch
        {
            BindingSource bs when !bs.CanAcceptDataFrom(FromRequestBindingSource.FromRequest) => null,
            BindingSource bs when bs.CanAcceptDataFrom(FromRequestBindingSource.FromRequest) => new FromRequestModelBinder(
                _bodyModelBinderProvider.GetBinder(context)!, _complexObjectModelBinderProvider.GetBinder(context)!),
            null => null,
            _ => null
        };
}
