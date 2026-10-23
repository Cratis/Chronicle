// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Serialization;

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Recognizes the failures a grain call produces when the silo on the other end runs a Chronicle release this one
/// cannot exchange grain calls with.
/// </summary>
/// <remarks>
/// During a rolling upgrade, silos of two releases call each other's grains. When the releases disagree on a grain
/// method or on a type that crosses between silos, the receiving silo cannot decode the call: Orleans reports a type
/// it cannot load, such as "Unable to resolve type alias", or a codec it does not have. Retrying does
/// not help, because nothing changes until every silo runs the same release, so these fail startup at once - but with
/// a message that says what is going on and what to do, instead of a bare type-load failure.
/// </remarks>
public static class IncompatibleSiloRelease
{
    /// <summary>
    /// Determines whether an exception, or one it wraps, most likely comes from a silo of an incompatible release.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to judge.</param>
    /// <returns><see langword="true"/> when the failure points at mixed releases rather than at the work itself.</returns>
    public static bool IsLikely(Exception exception) => Chain(exception).Any(_ => _ is TypeLoadException or CodecNotFoundException or UnsupportedWireTypeException);

    static IEnumerable<Exception> Chain(Exception exception)
    {
        var pending = new Stack<Exception>([exception]);
        while (pending.TryPop(out var current))
        {
            yield return current;

            if (current is AggregateException aggregate)
            {
                foreach (var inner in aggregate.InnerExceptions)
                {
                    pending.Push(inner);
                }
            }
            else if (current.InnerException is { } inner)
            {
                pending.Push(inner);
            }
        }
    }
}
