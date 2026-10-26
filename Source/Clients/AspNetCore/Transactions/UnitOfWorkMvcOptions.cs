// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.AspNetCore.Transactions;

/// <summary>
/// Registers the action signal and pre-result completion filters globally.
/// </summary>
/// <param name="chronicleOptions">The Chronicle ASP.NET Core options.</param>
internal sealed class UnitOfWorkMvcOptions(IOptions<ChronicleAspNetCoreOptions> chronicleOptions) : IConfigureOptions<MvcOptions>
{
    /// <inheritdoc/>
    public void Configure(MvcOptions options)
    {
        options.Filters.Add(new UnitOfWorkActionFilter(chronicleOptions));
        options.Filters.Add(new UnitOfWorkResultFilter(chronicleOptions));
    }
}
