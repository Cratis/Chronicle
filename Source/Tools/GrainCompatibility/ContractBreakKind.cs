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
    GrainClassRemoved = 5,

    /// <summary>
    /// A payload type is gone or moved, so a silo of the previous release can no longer resolve its wire name.
    /// </summary>
    PayloadTypeRemoved = 6,

    /// <summary>
    /// A payload changed codec or underlying type, so silos no longer agree on its serialized representation.
    /// </summary>
    PayloadKindChanged = 7,

    /// <summary>
    /// A payload member is gone or renamed, so a silo of the previous release loses a value it relies on.
    /// </summary>
    PayloadPropertyRemoved = 8,

    /// <summary>
    /// A payload member changed type, so a silo of the previous release cannot decode its value reliably.
    /// </summary>
    PayloadPropertyTypeChanged = 9,

    /// <summary>
    /// An enum member is gone, so a silo of the previous release can still send a value no longer understood.
    /// </summary>
    EnumMemberRemoved = 10,

    /// <summary>
    /// An enum member changed value, so silos interpret the same serialized value differently.
    /// </summary>
    EnumValueChanged = 11
}
