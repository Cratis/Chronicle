// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Tools.LegacyContracts;

record JobResult(string Result, IReadOnlyDictionary<string, string>? Outputs)
{
    [JsonIgnore]
    public bool Skipped => string.Equals(Result, "skipped", StringComparison.Ordinal) || (Outputs is not null && Outputs.TryGetValue("skipped", out var skipped) && string.Equals(skipped, "true", StringComparison.Ordinal));
}
