// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// The exception thrown when a multi-write sink cycle may or may not have committed and is too old to verify.
/// It is not retried: the observer partition fails visibly, so that an operator decides on a replay rather than the
/// cycle being applied a second time unnoticed.
/// </summary>
/// <param name="message">The message describing the cycle.</param>
public class SinkCycleOutcomeUnknown(string message) : Exception(message);
