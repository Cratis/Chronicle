// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Cratis.Chronicle.Benchmarks.SystemStoreGuard;

/// <summary>
/// Returns one request without the cost of a test double, so benchmarks measure the guard rather than the accessor.
/// </summary>
/// <param name="httpContext">The request every caller sees.</param>
public sealed class FixedHttpContextAccessor(HttpContext httpContext) : IHttpContextAccessor
{
    /// <inheritdoc/>
    public HttpContext? HttpContext
    {
        get => httpContext;
        set => throw new NotSupportedException("The benchmark request is fixed.");
    }
}
