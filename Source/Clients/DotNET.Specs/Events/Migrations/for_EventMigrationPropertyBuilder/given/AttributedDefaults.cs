// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Chronicle.Events.Migrations.for_EventTypeMigration.given;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder.given;

public record AttributedDefaults([property: JsonPropertyName("Explicit_Module")] ModuleKey Module, string Other);
