// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_replacing_isolated;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_replacing_isolated;

public class and_the_stored_watermark_is_newer() : Contract.a_conditional_replacement<SqlSinkHarness>(30, false, IsolatedReplaceResult.Superseded, 10);
