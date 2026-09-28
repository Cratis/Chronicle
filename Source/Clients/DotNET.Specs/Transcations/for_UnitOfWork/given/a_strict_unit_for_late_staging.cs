// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.given;

public class a_strict_unit_for_late_staging : a_unit_for_late_staging
{
    protected override UnitOfWorkLifecyclePolicy Policy => UnitOfWorkLifecyclePolicy.Strict;
}
