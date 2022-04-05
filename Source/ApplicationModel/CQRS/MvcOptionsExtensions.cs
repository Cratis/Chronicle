// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Applications.Commands;
using Aksio.Cratis.Applications.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for working with <see cref="MvcOptions"/>.
/// </summary>
public static class MvcOptionsExtensions
{
    /// <summary>
    /// Add CQRS setup.
    /// </summary>
    /// <param name="options"><see cref="MvcOptions"/> to build on.</param>
    /// <returns><see cref="MvcOptions"/> for building continuation.</returns>
    public static MvcOptions AddCQRS(this MvcOptions options)
    {
        options.Filters.Add<CommandActionFilter>();
        options.Filters.Add<QueryActionFilter>();
        return options;
    }
}
