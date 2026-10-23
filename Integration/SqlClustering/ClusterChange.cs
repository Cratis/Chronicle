// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.SqlClustering;

public enum ClusterChange
{
    Join = 0,
    Kill = 1,
    Restart = 2,
    RollingUpgrade = 3
}
