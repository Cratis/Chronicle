// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration;

/// <summary>
/// Captures a freshly inspected container exit state.
/// </summary>
/// <param name="ExitCode">The process exit code.</param>
/// <param name="OomKilled">Whether Docker killed the process for exceeding its memory limit.</param>
/// <param name="Error">The Docker state error.</param>
public sealed record ContainerExit(long ExitCode, bool OomKilled, string Error);
