// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Runtime.Messaging;

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Recognizes the failures a cross-silo grain call produces while a cluster is still forming.
/// </summary>
/// <remarks>
/// Every silo runs its own startup task, and those tasks make grain calls that can land on a sibling
/// silo. A sibling that is itself still starting - or that restarted a moment ago and has not been
/// reached yet - answers none of them, and the call fails after the default 30 second response
/// timeout. That is a transient state of a forming cluster, not a defect in the work being asked
/// for: the same call succeeds seconds later once membership settles.
/// <para>
/// These are the only failures worth retrying at startup. Everything else - a genuine bug in the
/// grain, storage that will not answer, a serialization mismatch - fails the same way on every
/// attempt, so retrying it only delays a real error.
/// </para>
/// </remarks>
public static class SiblingSiloInstability
{
    /// <summary>
    /// Determines whether an exception is a sibling silo that has not stabilized yet, rather than a
    /// failure of the work itself.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to judge.</param>
    /// <returns><see langword="true"/> when the call is worth retrying.</returns>
    public static bool IsTransient(Exception exception) => exception switch
    {
        TimeoutException => true,
        SiloUnavailableException => true,
        OrleansMessageRejectionException => true,
        ConnectionFailedException => true,
        _ => false
    };
}
