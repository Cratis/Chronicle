// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

#pragma warning disable SA1600
namespace Cratis.Kernel.Server;

/// <summary>
/// Represents a <see cref="IResourceFilter"/> for filtering out requests until the Kernel is ready.
/// </summary>
public class KernelReadyResourceFilter : IResourceFilter
{
    internal static bool KernelReady { get; set; }

    /// <inheritdoc/>
    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }

    /// <inheritdoc/>
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        if (KernelReady) return;
        context.Result = new NotFoundResult();
    }
}
