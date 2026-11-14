// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server;

/// <summary>
/// The file an enrollment code is read from. Once the code has been redeemed it is spent, but it is a credential for as long as it sits on disk.
/// </summary>
internal static class RedeemedEnrollmentCodeFile
{
    /// <summary>
    /// Remove the file of a redeemed code when this process can.
    /// </summary>
    /// <param name="path">The path of the file.</param>
    /// <returns>True when the file is gone, false when this process could not remove it.</returns>
    internal static bool TryRemove(string path)
    {
        try
        {
            File.Delete(path);
            return !File.Exists(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
