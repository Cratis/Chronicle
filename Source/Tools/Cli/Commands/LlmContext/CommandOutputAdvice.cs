// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.Commands.LlmContext;

/// <summary>
/// Advises which output format is most token-efficient for a specific command.
/// </summary>
/// <param name="Command">The full command name (e.g. "event-types list").</param>
/// <param name="RecommendedFormat">The most token-efficient output format for this command.</param>
/// <param name="Reason">Explanation of why this format is recommended.</param>
public record CommandOutputAdvice(string Command, string RecommendedFormat, string Reason);
