// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilder.given;

public record AttributedSourceContact([property: JsonPropertyName("WireAddress")] string EmailAddress, string FullName, string FirstName, string LastName, [property: JsonPropertyName("OldStatus")] string Status);
