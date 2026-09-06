// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when a call handing events to an observer's subscriber does not come back within the
/// configured subscriber timeout.
/// </summary>
/// <remarks>
/// Derives from <see cref="TimeoutException"/> so that it classifies as <c>FailureKind.Timeout</c> alongside the
/// transport's own timeout, and so that anything already handling a timeout keeps handling this one.
/// </remarks>
/// <param name="partition">The <see cref="Key">partition</see> whose events were being handed over.</param>
/// <param name="timeout">The timeout that elapsed.</param>
public class SubscriberCallTimedOut(Key partition, TimeSpan timeout)
    : TimeoutException($"The subscriber did not answer within {timeout} while handling events for partition '{partition}'");
