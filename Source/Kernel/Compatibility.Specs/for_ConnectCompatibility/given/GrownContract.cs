// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compatibility.for_WireCompatibilityChecker.given;

namespace Cratis.Chronicle.Compatibility.for_ConnectCompatibility.given;

/// <summary>
/// Builds a base contract and a superset of it with one extra field on the same message - the shape of
/// #4058's incident, where a client gained <c language="csharp">ReducerDefinition.Hash</c> that the kernel
/// it connected to had never declared.
/// </summary>
public static class GrownContract
{
    public const int AddedFieldNumber = 9;

    public static WireContract Base() => WireContracts.With();

    public static WireContract WithAddedField()
    {
        var baseContract = WireContracts.With();
        var message = baseContract.Messages[WireContracts.Message];

        return baseContract with
        {
            Messages = new Dictionary<string, WireMessage>(StringComparer.Ordinal)
            {
                [WireContracts.Message] = message with
                {
                    Fields = new Dictionary<int, WireField>(message.Fields)
                    {
                        [AddedFieldNumber] = new(AddedFieldNumber, "Hash", "string", WireFieldLabel.Singular, null)
                    }
                }
            }
        };
    }
}
