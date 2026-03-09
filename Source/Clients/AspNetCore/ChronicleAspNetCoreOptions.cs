// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Chronicle.AspNetCore.Namespaces;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Represents the ASP.NET Core–specific settings for connecting to Chronicle.
/// </summary>
/// <remarks>
/// Extends <see cref="Cratis.Chronicle.ChronicleClientOptions"/> with settings that are specific to
/// ASP.NET Core hosting, such as HTTP header–based namespace resolution. For non-web hosts (worker
/// services, console apps), use <see cref="Cratis.Chronicle.ChronicleClientOptions"/> directly via the
/// <c>IHostApplicationBuilder.AddCratisChronicle</c> extension in the DotNET client package.
/// </remarks>
public class ChronicleAspNetCoreOptions : Cratis.Chronicle.ChronicleClientOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleAspNetCoreOptions"/> class.
    /// </summary>
    public ChronicleAspNetCoreOptions()
    {
        EventStoreNamespaceResolverType = typeof(HttpHeaderEventStoreNamespaceResolver);
    }

    /// <summary>
    /// Gets or sets the name of the HTTP header to use for resolving the namespace.
    /// </summary>
    [Required]
    public string NamespaceHttpHeader { get; set; } = "x-cratis-tenant-id";
}
