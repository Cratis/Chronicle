// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_root_level_join.given;

public record GuidJoinedReadModel(string Id, Guid JoinedOn, string Stamped);
