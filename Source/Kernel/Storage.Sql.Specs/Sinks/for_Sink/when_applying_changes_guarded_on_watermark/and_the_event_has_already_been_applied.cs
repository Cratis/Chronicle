// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_applying_changes_guarded_on_watermark;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_guarded_on_watermark;

public class and_the_event_has_already_been_applied : Contract.and_the_event_has_already_been_applied<SqlSinkHarness>;
