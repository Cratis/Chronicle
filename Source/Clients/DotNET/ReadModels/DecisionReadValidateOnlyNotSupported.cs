// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>An older kernel rejected a validate-only decision append; the decision was not validated.</summary>
public class DecisionReadValidateOnlyNotSupported : Exception
{
    /// <summary>Creates the typed fail-closed result.</summary>
    public DecisionReadValidateOnlyNotSupported()
        : base("The kernel does not support validate-only decision reads.")
    {
    }
}
