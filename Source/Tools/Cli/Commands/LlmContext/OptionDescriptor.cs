// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.Commands.LlmContext;

/// <summary>
/// Describes a CLI option or argument.
/// </summary>
/// <param name="Name">The flag or argument name (e.g. "--server" or "&lt;OBSERVER_ID&gt;").</param>
/// <param name="Type">The value type (e.g. "string", "guid", "int").</param>
/// <param name="Description">A description of the option.</param>
public record OptionDescriptor(string Name, string Type, string Description);
