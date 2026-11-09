// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Schema;

/// <summary>
/// The outcome of probing a storage capability.
/// </summary>
public enum MongoDBCapabilityState
{
    /// <summary>
    /// The operation succeeded.
    /// </summary>
    Supported,

    /// <summary>
    /// The server explicitly rejected the feature.
    /// </summary>
    Unsupported,

    /// <summary>
    /// The probe could not determine support.
    /// </summary>
    Inconclusive
}
