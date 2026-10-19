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
/// <c language="csharp">IHostApplicationBuilder.AddCratisChronicle</c> extension in the DotNET client package.
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

    /// <summary>
    /// Gets the settings for the request unit of work.
    /// </summary>
    public UnitOfWorkOptions UnitOfWork { get; } = new();
}

/// <summary>
/// Represents the settings for the ASP.NET Core request unit of work.
/// </summary>
public class UnitOfWorkOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the middleware reports a failed commit as problem details (409 for
    /// conflicts, 400 for constraint violations, 500 otherwise) when the response has not started yet. Defaults to <see langword="false"/>.
    /// </summary>
    /// <remarks>Only applies to requests that did not claim completion through <c>IUnitOfWorkCompletionOwnershipFeature</c>.</remarks>
    public bool ReportFailuresBeforeResponseStarts { get; set; }
}
