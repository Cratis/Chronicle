// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Api.given;

/// <summary>
/// Base specification for API integration tests.
/// </summary>
/// <param name="fixture">The <see cref="ChronicleOutOfProcessFixtureWithLocalImage"/> fixture.</param>
public class Specification(ChronicleOutOfProcessFixtureWithLocalImage fixture) : Specification<ChronicleOutOfProcessFixtureWithLocalImage, ApiWebApplicationFactory, Program>(fixture);
