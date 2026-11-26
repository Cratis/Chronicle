// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Reports an exhausted MongoDB readiness budget without triggering generic container startup retries.
/// </summary>
/// <param name="message">The readiness failure and bounded diagnostics.</param>
[SuppressMessage("Design", "CA1064:Exceptions should be public", Justification = "Internal fixture failure; the published client API must remain unchanged.")]
internal sealed class MongoReplicaSetReadinessFailed(string message) : Exception(message);
