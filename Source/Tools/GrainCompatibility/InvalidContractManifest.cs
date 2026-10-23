// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrainCompatibility;

/// <summary>
/// The exception that is thrown when an OrleansContracts.txt cannot be read as a contract manifest.
/// </summary>
/// <param name="lineNumber">The 1-based line the problem was found on.</param>
/// <param name="message">What is wrong with it.</param>
public class InvalidContractManifest(int lineNumber, string message) : Exception($"Line {lineNumber}: {message}");
