// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_preparing_an_initial_run;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_preparing_an_initial_run;

public class and_the_read_model_already_has_state : Contract.and_the_read_model_already_has_state<SqlSinkHarness>;
