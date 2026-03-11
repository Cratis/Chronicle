// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.Commands.LlmContext;

/// <summary>
/// Describes the full CLI surface for AI agent consumption.
/// </summary>
public record LlmContextDescriptor
{
    /// <summary>
    /// Gets or sets the tool name.
    /// </summary>
    public required string Tool { get; set; }

    /// <summary>
    /// Gets or sets the version string.
    /// </summary>
    public required string Version { get; set; }

    /// <summary>
    /// Gets or sets a description of the tool.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Gets or sets options available on all commands.
    /// </summary>
    public required IReadOnlyList<OptionDescriptor> GlobalOptions { get; set; }

    /// <summary>
    /// Gets or sets the command groups and their commands.
    /// </summary>
    public required IReadOnlyList<CommandGroupDescriptor> CommandGroups { get; set; }

    /// <summary>
    /// Gets or sets connection configuration details.
    /// </summary>
    public required ConnectionInfoDescriptor ConnectionInfo { get; set; }

    /// <summary>
    /// Gets or sets usage tips for AI agents.
    /// </summary>
    public required IReadOnlyList<string> Tips { get; set; }
}
