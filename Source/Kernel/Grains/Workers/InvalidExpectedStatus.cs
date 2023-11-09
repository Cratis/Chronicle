// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Workers;

/// <summary>
/// Exception used to describe when the consumer is requests data
/// from the <see cref="ICpuBoundWorker{TRequest, TResult}"/>, when the grain
/// is not in a valid state to return the requested data.
/// </summary>
/// <remarks>
/// Based on the work done here: https://github.com/OrleansContrib/Orleans.SyncWork.
/// </remarks>
[GenerateSerializer]
public class InvalidExpectedStatus : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidExpectedStatus"/> class.
    /// </summary>
    /// <param name="actualStatus">The actual <see cref="CpuBoundWorkerStatus"/> that occurred.</param>
    /// <param name="expectedStatus">The expected <see cref="CpuBoundWorkerStatus"/> state that should have been received.</param>
    public InvalidExpectedStatus(CpuBoundWorkerStatus actualStatus, CpuBoundWorkerStatus expectedStatus)
        : base($"Grain was in an invalid state.  Expected status {expectedStatus}, got {actualStatus}.")
    {
    }
}
