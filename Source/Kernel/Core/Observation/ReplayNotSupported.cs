// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Base for refusals of a replay that must not run.
/// </summary>
/// <param name="message">The explanation of the refusal.</param>
internal abstract class ReplayNotSupported(string message) : InvalidOperationException(message);
