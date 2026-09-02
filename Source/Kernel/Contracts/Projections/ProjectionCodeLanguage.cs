// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents the language a projection is generated as client code in.
/// </summary>
/// <remarks>
/// C# is zero so that a request that leaves the field unset asks for C#, which is what every caller
/// got before there was a choice.
/// </remarks>
public enum ProjectionCodeLanguage
{
    /// <summary>
    /// C#, targeting the .NET client.
    /// </summary>
    CSharp = 0,

    /// <summary>
    /// TypeScript, targeting the TypeScript client.
    /// </summary>
    TypeScript = 1,

    /// <summary>
    /// Kotlin, targeting the JVM client.
    /// </summary>
    Kotlin = 2,

    /// <summary>
    /// Java, targeting the JVM client.
    /// </summary>
    Java = 3,

    /// <summary>
    /// Elixir, targeting the Elixir client.
    /// </summary>
    Elixir = 4
}
