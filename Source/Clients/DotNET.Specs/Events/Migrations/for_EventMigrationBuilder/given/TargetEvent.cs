// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilder.given;

public record TargetEvent(string Email, string FullName, string FirstName, int Status, TargetContact Contact);
