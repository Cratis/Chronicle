// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration;

/// <summary>
/// Indicates that every bounded startup attempt crashed.
/// </summary>
/// <param name="image">The container image.</param>
/// <param name="attempts">The evidence from each failed start.</param>
public sealed class ContainerStartupFailed(string image, IReadOnlyList<ContainerStartupAttempt> attempts)
    : Exception($"Container startup failed for {image}.{Environment.NewLine}{string.Join(Environment.NewLine, attempts.Select(attempt => attempt.Describe()))}", attempts[^1].Failure)
{
    /// <summary>
    /// Gets the failed startup attempts in order.
    /// </summary>
    public IReadOnlyList<ContainerStartupAttempt> Attempts { get; } = attempts;
}
