// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Cratis.Chronicle.Serverless;

/// <summary>
/// Defines when a serverless client registers its artifacts with the kernel.
/// </summary>
[Experimental("CHRX0001", UrlFormat = "https://www.cratis.io/docs/chronicle/clients/dotnet/serverless-mode")]
public enum ServerlessRegistration
{
    /// <summary>
    /// Another process registered the artifacts when the application was deployed. The client registers nothing.
    /// </summary>
    Deploy = 0,

    /// <summary>
    /// The client registers the artifacts itself when it starts cold. Intended for development.
    /// </summary>
    OnColdStart = 1
}
