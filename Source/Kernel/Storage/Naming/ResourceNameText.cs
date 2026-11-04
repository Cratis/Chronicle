// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;

namespace Cratis.Chronicle.Storage.Naming;

/// <summary>
/// Makes names safe to write to a log.
/// </summary>
static class ResourceNameText
{
    const int MaximumLoggedLength = 128;

    /// <summary>
    /// Escape every character that is not printable ASCII, and cap the length.
    /// </summary>
    /// <param name="value">The name.</param>
    /// <returns>The escaped name.</returns>
    public static string Escape(string value)
    {
        var builder = new StringBuilder();
        foreach (var character in value.Length > MaximumLoggedLength ? value[..MaximumLoggedLength] : value)
        {
            if (character is >= ' ' and <= '~')
            {
                builder.Append(character);
            }
            else
            {
                builder.Append("\\u").Append(((int)character).ToString("X4", CultureInfo.InvariantCulture));
            }
        }

        return builder.ToString();
    }
}
