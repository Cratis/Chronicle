// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Storage.Vault.for_VaultPathSegments.given;

/// <summary>
/// Helpers for specs of the path segments derived from encryption key address components.
/// </summary>
public class segments : Specification
{
    /// <summary>
    /// Text that must never be able to change which secret a segment addresses.
    /// </summary>
    protected static readonly string[] HostileCorpus =
    [
        "S/ns/../../X/p#",
        "..",
        ".",
        "a/../b",
        "a\\b",
        "a%2Fb",
        "%2e%2e",
        "a?b",
        "a#b",
        "a.",
        "a b",
        "\u0000",
        "\n",
        "\uFF0F",
        "~2F",
        "~",
        "~internal",
        "a/b",
        "a~2Fb",
        "\u00E9",
        "e\u0301",
        "A",
        "a",
        "\u00D8yvind"
    ];

    protected static string Encode(string value) => VaultPathSegments.Encode(value, "identifier");

    protected static Exception? Refusal(string value) => Catch.Exception(() => VaultPathSegments.Encode(value, "identifier"));

    /// <summary>
    /// Reverses the encoding, to prove it loses no information.
    /// </summary>
    /// <param name="segment">The encoded segment.</param>
    /// <returns>The original text.</returns>
    protected static string Decode(string segment)
    {
        var bytes = new List<byte>();
        for (var i = 0; i < segment.Length; i++)
        {
            if (segment[i] == '~')
            {
                bytes.Add(Convert.ToByte(segment.Substring(i + 1, 2), 16));
                i += 2;
            }
            else
            {
                bytes.Add((byte)segment[i]);
            }
        }

        return Encoding.UTF8.GetString([.. bytes]);
    }
}
