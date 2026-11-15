// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.SqlClustering.RollingUpgrade.EventRevisions;

public class when_19_126_uses_postgresql : ReleasedRevisionContract
{
    protected override string Image => ReleasedImages.Version19_126;
    protected override bool PostgreSql => true;
}
