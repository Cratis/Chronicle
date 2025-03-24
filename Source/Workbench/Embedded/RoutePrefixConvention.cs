// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Cratis.Chronicle.Workbench.Embedded;

/// <summary>
/// Represents an implementation of <see cref="IApplicationModelConvention"/> for giving route prefixes to API controllers.
/// </summary>
/// <param name="route">The <see cref="IRouteTemplateProvider"/>.</param>
public class RoutePrefixConvention(IRouteTemplateProvider route) : IApplicationModelConvention
{
    readonly AttributeRouteModel _routePrefix = new(route);

    /// <inheritdoc/>
    public void Apply(ApplicationModel application)
    {
        foreach (var selector in application.Controllers.SelectMany(c => c.Selectors))
        {
            if (selector.AttributeRouteModel != null)
            {
                selector.AttributeRouteModel = AttributeRouteModel.CombineAttributeRouteModel(_routePrefix, selector.AttributeRouteModel)!;
                selector.AttributeRouteModel.Template = _routePrefix.Template! + selector.AttributeRouteModel.Template;
            }
            else
            {
                selector.AttributeRouteModel = _routePrefix;
            }
        }
    }
}
