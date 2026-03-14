// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.Commands.LlmContext;

/// <summary>
/// Describes a command group (branch) in the CLI.
/// </summary>
/// <param name="Name">The branch name (e.g. "observers").</param>
/// <param name="Description">A description of the command group.</param>
/// <param name="Commands">The commands within this group.</param>
public record CommandGroupDescriptor(string Name, string Description, IReadOnlyList<CommandDescriptor> Commands);
