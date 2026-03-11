// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Cli;

/// <summary>
/// Holds the result of executing a CLI command, including exit code and captured output.
/// </summary>
/// <param name="ExitCode">The process exit code returned by the command.</param>
/// <param name="StandardOutput">The captured standard output text.</param>
/// <param name="StandardError">The captured standard error text.</param>
public record CliCommandResult(int ExitCode, string StandardOutput, string StandardError);
