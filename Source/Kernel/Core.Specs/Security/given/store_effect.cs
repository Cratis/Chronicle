// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.given;

public enum store_effect
{
    Reads = 0,
    Appends = 1,
    Configures = 2,
    Destroys = 3,
    Exports = 4,
    CrossStore = 5,
    CreatesDelegation = 6,
    Stream = 7,
    Enumerates = 8
}
