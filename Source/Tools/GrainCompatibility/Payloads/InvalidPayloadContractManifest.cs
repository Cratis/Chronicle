// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrainCompatibility.Payloads;

/// <summary>
/// The exception thrown when a grain payload manifest cannot be read.
/// </summary>
/// <param name="lineNumber">The 1-based line the problem was found on.</param>
/// <param name="message">What is wrong with it.</param>
public class InvalidPayloadContractManifest(int lineNumber, string message) : Exception($"Line {lineNumber}: {message}");
