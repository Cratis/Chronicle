// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_upserting_guarded;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_upserting_guarded;

public class and_the_watermark_is_older() : Contract.a_guarded_upsert<SqlSinkHarness>(10, 20);
