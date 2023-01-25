// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Aksio.Cratis.Applications.ModelBinding;

/// <summary>
/// Specifies that a parameter or property should be bound using the request body and allowing to be combined with route and query string using [FromRoute], [FromQuery] inside the object.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class FromRequestAttribute : Attribute, IBindingSourceMetadata
{
    /// <inheritdoc/>
    public BindingSource? BindingSource => new FromRequestBindingSource();
}
