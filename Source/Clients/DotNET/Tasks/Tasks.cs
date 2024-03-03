// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Tasks;

/// <summary>
/// Represents an implementation of <see cref="ITasks"/>.
/// </summary>
public class Tasks : ITasks
{
    /// <inheritdoc/>
    public Task Run(Func<Task> function, CancellationToken cancellationToken = default) => Task.Run(function, cancellationToken);

    /// <inheritdoc/>
    public Task Delay(int milliseconds, CancellationToken cancellationToken = default) => Task.Delay(milliseconds, cancellationToken);
}
