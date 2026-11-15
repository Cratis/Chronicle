// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.SqlClustering.RollingUpgrade.EventRevisions;

public class when_19_115_1_uses_sql_server : ReleasedRevisionContract
{
    /// <summary>Earlier kernels cannot connect through SqlClient; .github/rolling-upgrade-unusable-releases.txt excludes 19.115.0 for the key-ring deadlock (#5485).</summary>
    protected override string Image => ReleasedImages.Version19_115_1;
    protected override bool PostgreSql => false;
}
