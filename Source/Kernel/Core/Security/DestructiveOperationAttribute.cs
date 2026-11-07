// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// Marks an operation under the administrator operations policy whose effect discards or rewrites data and cannot be undone by the caller.
/// </summary>
/// <remarks>
/// The refusal covers the System store and kernel scope only; the same operation on an ordinary event store is left to that
/// store's own rules. Non-administrators are refused these operations when <see cref="Configuration.Authentication.EnforceDestructiveOperations"/> is on,
/// even while the broader administrator policy for operations is still in audit mode.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class DestructiveOperationAttribute : Attribute;
