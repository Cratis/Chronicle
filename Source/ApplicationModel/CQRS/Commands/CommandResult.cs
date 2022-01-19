// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Applications.Commands
{
    /// <summary>
    /// Represents the result coming from executing a command.
    /// </summary>
    /// <param name="IsOk">Whether or not everything is Ok.</param>
    public record CommandResult(bool IsOk);
}
