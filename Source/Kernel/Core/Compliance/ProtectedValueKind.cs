// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance;

/// <summary>The stable JSON kind codes authenticated by a v4 encryption envelope.</summary>
public enum ProtectedValueKind
{
    /// <summary>A string, stored verbatim rather than as quoted JSON.</summary>
    JsonString = 0,

    /// <summary>A JSON number, retaining its original JSON text.</summary>
    JsonNumber = 1,

    /// <summary>A JSON boolean.</summary>
    JsonBoolean = 2,

    /// <summary>A JSON array.</summary>
    JsonArray = 3,

    /// <summary>A JSON object.</summary>
    JsonObject = 4
}
