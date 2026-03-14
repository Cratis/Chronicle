// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli;

/// <summary>
/// Well-known output format identifiers for the CLI.
/// </summary>
public static class OutputFormats
{
    /// <summary>
    /// JSON output format.
    /// </summary>
    public const string Json = "json";

    /// <summary>
    /// Plain tab-separated output format.
    /// </summary>
    public const string Plain = "plain";

    /// <summary>
    /// Rich table output format (default for interactive terminals).
    /// </summary>
    public const string Text = "text";

    /// <summary>
    /// Automatic format detection based on terminal capabilities.
    /// </summary>
    public const string Auto = "auto";

    /// <summary>
    /// Compact (non-indented) JSON format — lower token count than <see cref="Json"/> while remaining fully machine-parseable.
    /// </summary>
    public const string JsonCompact = "json-compact";
}
