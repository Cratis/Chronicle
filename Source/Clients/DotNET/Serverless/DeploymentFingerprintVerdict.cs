// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Serverless;

/// <summary>
/// The advisory deployment comparison result.
/// </summary>
internal enum DeploymentFingerprintVerdict
{
    /// <summary>The stored deployment matches the prepared definitions.</summary>
    UpToDate = 0,

    /// <summary>No deployment metadata has been stored.</summary>
    NotRegistered = 1,

    /// <summary>The stored deployment differs from the prepared definitions.</summary>
    Stale = 2
}
