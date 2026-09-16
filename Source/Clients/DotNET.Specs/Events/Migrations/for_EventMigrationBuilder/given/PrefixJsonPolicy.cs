// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilder.given;

public class PrefixJsonPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name) => $"mapped_{name}";
}
