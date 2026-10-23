// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// Signals that an atomic attempt must abort before exceeding the destination document cap.
/// </summary>
public sealed class SinkBatchLimitExceeded : Exception;
