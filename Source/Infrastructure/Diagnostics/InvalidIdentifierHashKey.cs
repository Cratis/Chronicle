// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// The exception that is thrown when an identifier hash key is missing or too short.
/// </summary>
public class InvalidIdentifierHashKey() : Exception("Identifier hash key must be at least 32 characters");
