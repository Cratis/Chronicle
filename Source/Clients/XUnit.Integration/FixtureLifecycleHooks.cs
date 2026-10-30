// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Containers;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Provides fixture-instance lifecycle observations and fault injection for owned-resource specifications.
/// </summary>
internal sealed class FixtureLifecycleHooks
{
    /// <summary>
    /// Gets the optional owned TLS material factory.
    /// </summary>
    internal Func<KernelFixtureCertificate>? CreateMaterial { get; init; }

    /// <summary>
    /// Gets the observation before each build attempt.
    /// </summary>
    internal Action? BeforeBuild { get; init; }

    /// <summary>
    /// Gets the start wrapper with the actual container and its native start operation.
    /// </summary>
    internal Func<IContainer, Func<Task>, Task>? Start { get; init; }

    /// <summary>
    /// Gets or sets the disposal wrapper with the actual container and its native disposal operation.
    /// </summary>
    internal Func<IContainer, Func<ValueTask>, ValueTask>? Dispose { get; set; }

    /// <summary>
    /// Gets the observation of each built kernel or auxiliary container.
    /// </summary>
    internal Action<IContainer>? ContainerBuilt { get; init; }

    /// <summary>
    /// Gets the optional replacement for the reset client's certificate pin.
    /// </summary>
    internal Func<string, string>? ResetPin { get; init; }

    /// <summary>
    /// Gets the observation of the actual issued native reset task.
    /// </summary>
    internal Action<Task>? ResetIssued { get; init; }
}
