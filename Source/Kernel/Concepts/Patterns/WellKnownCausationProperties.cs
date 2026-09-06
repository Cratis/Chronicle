// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Patterns;

/// <summary>
/// Holds the causation property names pattern mining reads when they are present.
/// </summary>
/// <remarks>
/// A causation's type says what kind of link it is - an HTTP request, a reactor, a command - and every command
/// shares the one type. What behavior is mined by is which command, so mining prefers a property naming it and
/// falls back to the type when nothing named itself. The names are a convention shared with the layer above
/// Chronicle: Arc's command pipeline records them, and anything else that executes named work can too.
/// </remarks>
public static class WellKnownCausationProperties
{
    /// <summary>
    /// The property naming the command a causation link represents.
    /// </summary>
    public const string CommandType = "commandType";
}
