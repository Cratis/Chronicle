// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

public enum UnknownCommitMode
{
    CommittedButReplyLost = 0,
    AbortedButReplyLost = 1,
    LeftOpenAndReplyLost = 2
}
