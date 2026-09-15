// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections;

/// <summary>
/// Represents the language a projection is generated as client code in.
/// </summary>
/// <remarks>
/// C# is the default so that a caller that does not say which language it wants keeps the behavior
/// it had before there was a choice.
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
    /// <remarks>
    /// The JVM client does not expose a projection API to Java yet, so nothing generates for it.
    /// It is named here so that adding the generator later needs no change to the wire contract.
    /// </remarks>
    Java = 3,

    /// <summary>
    /// Elixir, targeting the Elixir client.
    /// </summary>
    Elixir = 4
}
