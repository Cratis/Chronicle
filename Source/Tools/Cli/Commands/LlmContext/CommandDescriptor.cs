// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.Commands.LlmContext;

/// <summary>
/// Describes a single CLI command within a group.
/// </summary>
/// <param name="Name">The command name (e.g. "list").</param>
/// <param name="Description">A description of the command.</param>
/// <param name="InheritedOptions">Options inherited from the parent (event store settings, etc.).</param>
/// <param name="Options">Command-specific options and arguments.</param>
public record CommandDescriptor(string Name, string Description, IReadOnlyList<OptionDescriptor>? InheritedOptions, IReadOnlyList<OptionDescriptor>? Options);
