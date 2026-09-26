// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Kernel;

/// <summary>
/// The exception that is thrown when a kernel projection declaration does not carry a <see cref="KernelProjectionAttribute"/>.
/// </summary>
/// <param name="type">The declaring type.</param>
/// <remarks>
/// The identifier is what the definition is stored, retired and recognized under, so it cannot be inferred from a
/// type name that is free to change. Failing at startup is deliberate: a projection that silently declined to
/// register would leave whatever reads its read model quietly answering nothing.
/// </remarks>
public class KernelProjectionWithoutIdentifier(Type type)
    : Exception($"Kernel projection '{type.FullName}' is missing the [KernelProjection] attribute that gives it its identifier");
