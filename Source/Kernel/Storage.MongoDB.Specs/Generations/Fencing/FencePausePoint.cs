// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

public enum FencePausePoint
{
    None = 0,
    BeforeAdmission = 1,
    AfterAdmission = 2,
    MidExecution = 3,
    BeforeCommit = 4
}
