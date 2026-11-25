// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_finding_keys_holding_a_child;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_finding_keys_holding_a_child;

public class and_no_parent_holds_it : Contract.and_no_parent_holds_it<SqlSinkHarness>;
