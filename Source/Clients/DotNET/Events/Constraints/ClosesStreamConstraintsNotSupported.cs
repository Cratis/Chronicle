// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// The exception that is thrown when a constraint builder implementation does not support closing events.
/// </summary>
public class ClosesStreamConstraintsNotSupported() : Exception("This constraint builder implementation does not support closing-event constraints.");
