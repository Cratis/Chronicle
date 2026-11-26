// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Helpers.for_CustomHttpHeaders.given;

public class invalid_headers : Specification
{
    protected const string Secret = "synthetic-header-secret-6136";

    protected readonly Dictionary<string, string>[] _invalidHeaders =
    [
        new() { ["X-Custom"] = $"{Secret}\r\ninjected" },
        new() { ["X-Custom"] = $"{Secret}\ninjected" },
        new() { ["X-Custom"] = $"{Secret}\rinjected" },
        new() { ["Date"] = Secret },
        new() { ["invalid header"] = Secret },
        new() { ["Content-Type"] = Secret }
    ];
}
