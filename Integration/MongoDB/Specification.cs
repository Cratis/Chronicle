// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.MongoDB.Integration;

/// <summary>
/// Lightweight base specification for MongoDB integration tests.
/// Does not start an Orleans silo — only the MongoDB container is required.
/// </summary>
/// <param name="fixture">The <see cref="ChronicleInProcessFixture"/> providing the MongoDB container.</param>
public abstract class Specification(ChronicleInProcessFixture fixture) : MongoDBSpecification(fixture);
