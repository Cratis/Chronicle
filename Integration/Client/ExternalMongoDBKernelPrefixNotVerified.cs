// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration;

/// <summary>
/// The exception that is thrown when an external MongoDB kernel's database isolation cannot be verified before reset.
/// </summary>
/// <param name="expectedDatabase">The prefixed cluster database that must exist.</param>
/// <param name="unexpectedDatabases">Unprefixed Chronicle databases created since startup.</param>
public class ExternalMongoDBKernelPrefixNotVerified(string expectedDatabase, IEnumerable<string> unexpectedDatabases)
    : Exception($"Refusing to reset an external MongoDB server: expected '{expectedDatabase}' to exist and no new unprefixed Chronicle databases. New unprefixed databases: [{string.Join(", ", unexpectedDatabases)}]. An older kernel image may ignore DatabaseNamePrefix and delete unrelated databases. Build the current kernel image and set CRATIS_CHRONICLE_LOCAL_IMAGE before running these tests.");
