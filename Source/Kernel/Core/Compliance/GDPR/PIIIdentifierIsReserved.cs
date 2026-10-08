// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.GDPR;

/// <summary>
/// The exception that is thrown when PII addresses an identifier reserved for confidentiality keys.
/// </summary>
/// <param name="identifier">The reserved identifier.</param>
public class PIIIdentifierIsReserved(string identifier)
    : Exception($"The identifier '{identifier}' is reserved for confidentiality keys and cannot protect PII.");
