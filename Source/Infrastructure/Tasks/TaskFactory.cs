// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Tasks;

/// <summary>
/// Represents an implementation of <see cref="ITaskFactory"/>.
/// </summary>
public class TaskFactory : ITaskFactory
{
    /// <inheritdoc/>
    public Task Run(Func<Task> function) => Task.Run(function);

    /// <inheritdoc/>
    public Task Delay(int milliseconds) => Task.Delay(milliseconds);
}
