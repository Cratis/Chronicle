// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Operations;

/// <summary>
/// Defines a system for a task in the system.
/// </summary>
/// <typeparam name="TRequest">Type of request for the task.</typeparam>
public interface IOperation<TRequest> : IGrainWithGuidCompoundKey
    where TRequest : class
{
    /// <summary>
    /// Execute the task.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Perform();
}
