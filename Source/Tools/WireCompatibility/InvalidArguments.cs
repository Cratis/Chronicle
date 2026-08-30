// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.WireCompatibility;

/// <summary>
/// The exception that is thrown when the command line does not describe a comparison that can be made.
/// </summary>
/// <param name="message">What is wrong with it.</param>
public class InvalidArguments(string message) : Exception(message);
