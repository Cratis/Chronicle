// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Headers;

namespace Cratis.Chronicle.Helpers;

/// <summary>
/// Applies custom HTTP headers without exposing invalid values in parser exceptions.
/// </summary>
internal static class CustomHttpHeaders
{
    /// <summary>
    /// Adds a header using the request header parser and reports only its name on failure.
    /// </summary>
    /// <param name="headers">The request headers.</param>
    /// <param name="name">The custom header name.</param>
    /// <param name="value">The custom header value.</param>
    /// <exception cref="InvalidCustomHttpHeader">The header name or value is invalid.</exception>
    internal static void Add(HttpRequestHeaders headers, string name, string value)
    {
        try
        {
            headers.Add(name, value);
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException or InvalidOperationException)
        {
            throw new InvalidCustomHttpHeader(name);
        }
    }
}
