// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator;

/// <summary>
/// Writes generated files without changing timestamps when their bytes are unchanged.
/// </summary>
public static class GeneratedFile
{
    /// <summary>
    /// Writes UTF-8 text without a byte order mark when its content differs from the file.
    /// </summary>
    /// <param name="path">The file to write.</param>
    /// <param name="content">The generated content.</param>
    /// <returns>Whether the file was written.</returns>
    public static bool WriteIfChanged(string path, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        if (File.Exists(path) && File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes))
        {
            return false;
        }

        File.WriteAllBytes(path, bytes);
        return true;
    }
}
