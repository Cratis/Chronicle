// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// The exception that is thrown when a fenced batch keeps failing with transient transaction errors.
/// </summary>
/// <param name="token">The <see cref="FenceToken"/> the batch was attempted with.</param>
/// <param name="attempts">The number of attempts made.</param>
/// <param name="innerException">The last transient error.</param>
public class FencedBatchRetriesExhausted(FenceToken token, int attempts, Exception innerException)
    : Exception($"A fenced batch through '{token.GateId}' failed with a transient transaction error {attempts} times; none of the attempts committed.", innerException);
