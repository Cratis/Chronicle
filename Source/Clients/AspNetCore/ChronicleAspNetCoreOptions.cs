// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Chronicle;
using Cratis.Chronicle.Connections;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Represents the settings for connecting to Chronicle.
/// </summary>
public class ChronicleAspNetCoreOptions
{
    /// <summary>
    /// Gets or sets the <see cref="Url"/> to use.
    /// </summary>
    public ChronicleUrl Url { get; set; } = ChronicleUrl.Default;

    /// <summary>
    /// Gets or sets the name of the event store to use.
    /// </summary>
    [Required]
    public EventStoreName EventStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the HTTP header to use for resolving the namespace.
    /// </summary>
    [Required]
    public string NamespaceHttpHeader { get; set; } = "x-cratis-tenant-id";

    /// <summary>
    /// Gets or sets the software version.
    /// </summary>
    public string SoftwareVersion { get; set; } = "0.0.0";

    /// <summary>
    /// Gets or sets the software commit.
    /// </summary>
    public string SoftwareCommit { get; set; } = "[N/A]";

    /// <summary>
    /// Gets or sets the program identifier.
    /// </summary>
    public string ProgramIdentifier { get; set; } = "[N/A]";
}
