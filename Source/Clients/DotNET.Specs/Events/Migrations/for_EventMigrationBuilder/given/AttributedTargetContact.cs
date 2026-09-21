// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilder.given;

public record AttributedTargetContact([property: JsonPropertyName("WireEmail")] string Email, string FullName, string FirstName, [property: JsonPropertyName("NewStatus")] string Status, string Label);
