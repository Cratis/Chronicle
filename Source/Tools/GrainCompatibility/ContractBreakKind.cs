// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrainCompatibility;

/// <summary>
/// The ways a grain contract can stop being served by the release after it.
/// </summary>
public enum ContractBreakKind
{
    /// <summary>
    /// A grain interface of the baseline is gone or retired, so calls the baseline makes through it cannot be decoded.
    /// </summary>
    InterfaceRemoved = 0,

    /// <summary>
    /// A method of the baseline is gone, which includes changing its parameter types: that gives it a new wire identity.
    /// </summary>
    MethodRemoved = 1,

    /// <summary>
    /// A method kept its wire identity but its parameter or return types changed, so the payload no longer matches.
    /// </summary>
    MethodSignatureChanged = 2,

    /// <summary>
    /// An interface gained methods without a higher [Version], so Orleans can route a call that needs a
    /// new method to an activation on a silo that does not have it.
    /// </summary>
    MethodsAddedWithoutVersionIncrease = 3,

    /// <summary>
    /// An interface's [Version] went down.
    /// </summary>
    VersionDecreased = 4,

    /// <summary>
    /// A grain class of the baseline is gone or retired, or its grain type changed, so grains the baseline activates
    /// or persists under that identity can no longer be found.
    /// </summary>
    GrainClassRemoved = 5
}
