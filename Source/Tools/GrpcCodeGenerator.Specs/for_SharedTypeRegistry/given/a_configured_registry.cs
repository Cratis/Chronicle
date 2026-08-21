// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_SharedTypeRegistry.given;

/// <summary>
/// Configures <see cref="SharedTypeRegistry"/> the same way <c>Program</c> does for a real run - the registry is
/// static (a single-shot CLI process has no concurrency to guard against, the same simplification
/// <see cref="TransportTypes"/> relies on), so every spec re-establishes known configuration rather than relying
/// on whatever a previous spec left behind.
/// </summary>
public class a_configured_registry : Specification
{
    void Establish() => SharedTypeRegistry.Configure(2, "Cratis.Chronicle.Contracts");
}
