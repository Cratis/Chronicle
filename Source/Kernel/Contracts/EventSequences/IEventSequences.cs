// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ServiceModel;

namespace Aksio.Cratis.Kernel.Contracts.EventSequences;

/// <summary>
/// Defines the contract for working with event sequences.
/// </summary>
[ServiceContract]
public interface IEventSequences
{
    /// <summary>
    /// Append an event to an event sequence.
    /// </summary>
    /// <param name="request">The <see cref="AppendRequest"/>.</param>
    /// <returns>The <see cref="AppendResponse"/>.</returns>
    [OperationContract]
    Task<AppendResponse> Append(AppendRequest request);
}
