// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the configuration for how the Workbench is served.
/// </summary>
public class WorkbenchOptions
{
    /// <summary>
    /// Gets or inits the path the Workbench is served under, for a deployment behind a reverse proxy that routes by path.
    /// </summary>
    /// <remarks>
    /// Empty by default, which serves the Workbench at the root of the host. A value such as
    /// <c language="csharp">/my-instance/chronicle</c> serves the page, its assets and the API it calls under that prefix, and writes the
    /// prefix into the page so the browser requests everything through it. The prefix may be stripped by the proxy or
    /// passed through to the kernel - both work. Configure through <c language="csharp">Cratis__Chronicle__Workbench__BasePath</c>
    /// in the environment.
    /// <para>
    /// This is an explicit setting rather than something taken from <c language="csharp">X-Forwarded-Prefix</c>. Anything that already
    /// places the Workbench behind a prefix of its own and rewrites the page on the way out - the Cratis Stage does -
    /// leaves it empty and keeps working unchanged.
    /// </para>
    /// </remarks>
    public string BasePath { get; init; } = string.Empty;
}
