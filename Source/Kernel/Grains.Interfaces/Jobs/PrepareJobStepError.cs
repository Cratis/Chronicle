// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// The type of error that occurred while performing prepare operation on <see cref="IJobStep"/>.
/// </summary>
public enum PrepareJobStepError
{
    /// <summary>
    /// Unknown error occurred.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The request type was wrong.
    /// </summary>
    WrongRequestType = 1,

    /// <summary>
    /// The job step cannot be prepared for some reason.
    /// </summary>
    CannotPrepare = 2,

    /// <summary>
    /// An unexpected error while preparing job step.
    /// </summary>
    UnexpectedErrorPreparing = 3
}