// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// The exception that is thrown when an event sequence implementation does not support closed stream scopes.
/// </summary>
public class ClosedStreamScopesNotSupported() : Exception("This event sequence implementation does not support closed stream scopes.");
