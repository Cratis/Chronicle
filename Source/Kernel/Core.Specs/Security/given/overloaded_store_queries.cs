// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.given;

/// <summary>
/// Provides overloads without a ReadModel attribute; this fixture is not a production query surface.
/// </summary>
public static class overloaded_store_queries
{
    public static string Query(string eventStore, int value) => $"{eventStore}{value}";

    public static string Query(int eventStore, string value) => $"{eventStore}{value}";
}
