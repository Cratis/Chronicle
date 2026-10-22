// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrainCompatibility;

/// <summary>
/// A grain interface method as recorded in an OrleansContracts.txt.
/// </summary>
/// <param name="Identity">The wire identity Orleans uses for the method: a generated hash, or a source [Id] or [Alias].</param>
/// <param name="Name">The CLR name of the method.</param>
/// <param name="Signature">The parameter and return types, without the method name.</param>
public record GrainMethodContract(string Identity, string Name, string Signature)
{
    /// <summary>
    /// Gets the method as it reads in the manifest.
    /// </summary>
    public string Display => $"{Identity}: {Name}{Signature}";
}
