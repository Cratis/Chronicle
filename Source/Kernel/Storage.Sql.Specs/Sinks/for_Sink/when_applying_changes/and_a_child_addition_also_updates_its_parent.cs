// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_applying_changes;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes;

public class and_a_child_addition_also_updates_its_parent : Contract.and_a_child_addition_also_updates_its_parent<SqlSinkHarness>;
