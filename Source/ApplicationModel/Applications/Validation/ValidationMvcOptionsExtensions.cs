// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Applications;
using Aksio.Cratis.Applications.Validation;
using Aksio.Cratis.Types;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for working with <see cref="MvcOptions"/>.
/// </summary>
public static class ValidationMvcOptionsExtensions
{
    /// <summary>
    /// Add CQRS setup.
    /// </summary>
    /// <param name="options"><see cref="MvcOptions"/> to build on.</param>
    /// <param name="types"><see cref="ITypes"/> for type discovery.</param>
    /// <returns><see cref="MvcOptions"/> for building continuation.</returns>
    public static MvcOptions AddValidation(this MvcOptions options, ITypes types)
    {
        options.ModelValidatorProviders.Add(new DiscoverableModelValidatorProvider(types, Internals.ServiceProvider!));
        return options;
    }
}
