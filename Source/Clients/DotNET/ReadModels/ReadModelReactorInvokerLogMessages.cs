// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ReadModels;

internal static partial class ReadModelReactorInvokerLogMessages
{
    [LoggerMessage(LogLevel.Warning, "Read model reactor returned a value of type '{ReturnType}' that no side-effect handler could process")]
    internal static partial void ReadModelReactorReturnValueNotHandled(this ILogger<ReadModelReactorInvoker> logger, string returnType);

    [LoggerMessage(LogLevel.Error, "Read model reactor side-effect append failed: {Messages}")]
    internal static partial void ReadModelReactorSideEffectAppendFailed(this ILogger<ReadModelReactorInvoker> logger, string messages);
}
