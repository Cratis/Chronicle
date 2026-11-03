// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder.given;

[JsonConverter(typeof(CustomWrittenConverter))]
public record CustomWritten(string Value);
