// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.Commands.LlmContext;

/// <summary>
/// Describes per-command output format guidance for AI agents to minimize token usage.
/// </summary>
public record OutputFormatGuidanceDescriptor
{
    /// <summary>
    /// Gets or sets a summary of the general guidance.
    /// </summary>
    public required string Summary { get; set; }

    /// <summary>
    /// Gets or sets per-command advice on which output format to use.
    /// </summary>
    public required IReadOnlyList<CommandOutputAdvice> PerCommand { get; set; }
}
