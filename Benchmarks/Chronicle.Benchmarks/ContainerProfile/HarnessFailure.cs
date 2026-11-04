// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>A harness failure with a safe, credential-free message.</summary>
/// <param name="message">The safe diagnostic.</param>
internal sealed class HarnessFailure(string message) : Exception(message);
