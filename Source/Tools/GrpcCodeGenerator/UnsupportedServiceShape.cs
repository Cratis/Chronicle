// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator;

/// <summary>
/// The exception that is thrown when an artifact has a shape the implementation generator cannot dispatch to.
/// </summary>
/// <remarks>
/// Refusing is deliberate. The alternative - emitting something approximate - produces an implementation that
/// compiles and serves the wrong thing, which is exactly the class of silent failure the generated contracts
/// exist to remove.
/// </remarks>
/// <param name="artifact">The artifact that cannot be dispatched to.</param>
/// <param name="reason">Why it cannot be dispatched to.</param>
public class UnsupportedServiceShape(string artifact, string reason)
    : Exception($"'{artifact}' cannot have an implementation generated for it: {reason}");
