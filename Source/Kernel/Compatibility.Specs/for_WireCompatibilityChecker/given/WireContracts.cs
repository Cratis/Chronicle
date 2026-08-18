// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compatibility.for_WireCompatibilityChecker.given;

/// <summary>
/// Builds small wire contracts by hand, so each specification states exactly the one difference it is about.
/// </summary>
public static class WireContracts
{
    public const string Service = ".test.Things";
    public const string Message = ".test.Thing";
    public const string Enum = ".test.Colour";

    public static WireContract With(
        WireMethod? method = null,
        WireField? field = null,
        (int Number, string Name)? enumValue = null) =>
        new(
            new Dictionary<string, WireService>(StringComparer.Ordinal)
            {
                [Service] = new(Service, new Dictionary<string, WireMethod>(StringComparer.Ordinal)
                {
                    [(method ?? DefaultMethod).Name] = method ?? DefaultMethod
                })
            },
            new Dictionary<string, WireMessage>(StringComparer.Ordinal)
            {
                [Message] = new(Message, new Dictionary<int, WireField>
                {
                    [(field ?? DefaultField).Number] = field ?? DefaultField
                })
            },
            new Dictionary<string, WireEnum>(StringComparer.Ordinal)
            {
                [Enum] = new(Enum, new Dictionary<int, string>
                {
                    [(enumValue ?? DefaultEnumValue).Number] = (enumValue ?? DefaultEnumValue).Name
                })
            });

    public static WireContract Empty() => new(
        new Dictionary<string, WireService>(StringComparer.Ordinal),
        new Dictionary<string, WireMessage>(StringComparer.Ordinal),
        new Dictionary<string, WireEnum>(StringComparer.Ordinal));

    public static WireMethod DefaultMethod { get; } = new("Do", Message, Message, ClientStreaming: false, ServerStreaming: false);

    public static WireField DefaultField { get; } = new(1, "Name", "string", WireFieldLabel.Singular, OneOf: null);

    public static (int Number, string Name) DefaultEnumValue => (0, "Red");
}
