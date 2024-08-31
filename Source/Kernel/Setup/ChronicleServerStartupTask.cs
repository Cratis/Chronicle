// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting;

/// <summary>
/// Represents a startup task for Chronicle.
/// </summary>
public class ChronicleServerStartupTask : IStartupTask
{
    /// <inheritdoc/>
    public Task Execute(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
