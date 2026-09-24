// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties;

/// <summary>
/// Exception thrown when a derived property function is referenced in a property path but not recognized.
/// </summary>
/// <param name="functionName">The name of the unknown function.</param>
public class UnknownDerivedPropertyFunction(string functionName)
    : Exception($"The derived property function '{functionName}' is not recognized. Register it in DerivedPropertyFunctions.All.");
