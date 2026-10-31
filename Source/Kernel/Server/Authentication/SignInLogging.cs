// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication;

internal static partial class SignInLogging
{
    [LoggerMessage(LogLevel.Warning, "Password sign-in deferred for {Username} from {Address}")]
    internal static partial void SignInThrottled(this ILogger logger, string username, string address);
}
