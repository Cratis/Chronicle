// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.AspNetCore.Namespaces;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.AspNetCore;

/// <summary>
/// Reports ASP.NET Core specific Chronicle setup mistakes once the host has started.
/// </summary>
/// <param name="marker">The <see cref="ChronicleUseMarker"/> recording whether <c language="csharp">UseCratisChronicle</c> was called.</param>
/// <param name="namespaceResolver">The <see cref="IEventStoreNamespaceResolver"/> in effect.</param>
/// <param name="headerName">Name of the HTTP header the namespace is read from by default.</param>
internal sealed class ChronicleAspNetCoreStartupDiagnostics(
    ChronicleUseMarker marker,
    IEventStoreNamespaceResolver namespaceResolver,
    string headerName)
{
    /// <summary>
    /// Report anything suspicious about the setup.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to report to.</param>
    public void Report(ILogger logger)
    {
        if (!marker.Called)
        {
            logger.UseCratisChronicleNotCalled();
        }

        if (namespaceResolver is HttpHeaderEventStoreNamespaceResolver)
        {
            logger.NamespaceFromHttpHeader(headerName);
        }
    }
}
