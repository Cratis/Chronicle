// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable RCS1110, CA1050, MA0047

/// <summary>
/// Extension methods for working with exceptions.
/// </summary>
public static class ExceptionExtensions
{
    /// <summary>
    /// Get all messages recursively throughout all inner exceptions.
    /// </summary>
    /// <param name="exception">The Exception to get from.</param>
    /// <returns>A collection of messages.</returns>
    public static IEnumerable<string> GetAllMessages(this Exception exception)
    {
        var exceptionMessages = new List<string>();

        do
        {
            exceptionMessages.Add(exception.Message);
            exception = exception.InnerException!;
        }
        while (exception is not null);

        return exceptionMessages;
    }
}
