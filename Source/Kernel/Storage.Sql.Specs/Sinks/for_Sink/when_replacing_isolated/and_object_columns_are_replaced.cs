// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_replacing_isolated;

public class and_object_columns_are_replaced : when_object_columns_are_replaced
{
    protected override bool UseIsolatedReplace => true;

    protected override ISqlSinkHarness CreateHarness() => new SqlSinkHarness();
}
