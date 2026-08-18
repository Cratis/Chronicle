// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_ending_a_replay;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ending_a_replay;

public class and_a_second_replay_follows_the_first : Contract.and_a_second_replay_follows_the_first<SqlSinkHarness>;
