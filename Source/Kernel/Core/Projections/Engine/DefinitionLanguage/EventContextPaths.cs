// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage;

/// <summary>
/// Decides whether an <c language="csharp">$eventContext</c> path can be read, the same way the projection engine reads it.
/// </summary>
/// <remarks>
/// Screenplay's event context catalog is deliberately incomplete and only warns about a path it does not list
/// (PLAY0295 and PLAY0296). The engine reads an event context value by reflecting over Chronicle's own
/// <see cref="EventContext"/> through <see cref="PropertyPath"/>, including its PascalCase fallback and the
/// well-known <see cref="DerivedPropertyFunctions"/>, so that is the authority on whether a path resolves.
/// </remarks>
internal static class EventContextPaths
{
    /// <summary>
    /// Gets the error for an event context path the engine cannot read, if any.
    /// </summary>
    /// <param name="path">The path below <c language="csharp">$eventContext</c>, for example <c language="csharp">occurred.year</c>.</param>
    /// <returns>The error message, or null when the engine can read the path.</returns>
    public static string? GetResolutionError(string path)
    {
        if (CanResolve(path))
        {
            return null;
        }

        var member = path.Split('.')[0];
        return CanResolve(member)
            ? $"Unknown event context path '$eventContext.{path}' - Chronicle's event context cannot resolve it"
            : $"Unknown event context member '{member}' in '$eventContext.{path}' - Chronicle's event context has no such member";
    }

    static bool CanResolve(string path)
    {
        var segments = path.Split('.');
        var lastSegment = segments[^1];
        var functionName = lastSegment.EndsWith("()", StringComparison.Ordinal) ? lastSegment[..^2] : lastSegment;

        // A terminal derived function (for example 'occurred.Week') is evaluated against the value the rest
        // of the path resolves to, so it is the receiver that has to resolve on the event context.
        var receiver = DerivedPropertyFunctions.TryGet(functionName, out _) ? segments[..^1] : segments;
        if (receiver.Length == 0)
        {
            return false;
        }

        try
        {
            new PropertyPath(string.Join('.', receiver)).GetPropertyInfoFor<EventContext>();
            return true;
        }
        catch (Exception exception) when (exception is UnableToResolvePropertyPathOnType or UnknownDerivedPropertyFunction)
        {
            // The engine would fail to read this path at runtime for the same reason, so it is not resolvable.
            return false;
        }
    }
}
