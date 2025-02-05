// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OneOf.Types;
namespace Cratis.Chronicle.Concepts.Jobs;

/// <summary>
/// Defines a system that knows about <see cref="JobType"/> job types and how to correlate it to CLR <see cref="Type"/>.
/// </summary>
public interface IJobTypes
{
    /// <summary>
    /// Error types for <see cref="IJobTypes.GetRequestClrTypeFor"/>.
    /// </summary>
    enum GetRequestClrTypeForError
    {
        JobTypeIsNotSet = 0,
        CouldNotFindType = 1
    }

    /// <summary>
    /// Error types for <see cref="IJobTypes.GetClrTypeFor"/>.
    /// </summary>
    enum GetClrTypeForError
    {
        JobTypeIsNotSet = 0,
        CouldNotFindType = 1
    }

    /// <summary>
    /// Error types for <see cref="IJobTypes.GetFor"/>.
    /// </summary>
    enum GetForError
    {
        NoAssociatedJobType = 0
    }

    /// <summary>
    /// Gets the <see cref="JobType"/> associated with the CLR <see cref="Type"/> or <see cref="None"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/>.</param>
    /// <returns><see cref="Result{T0, T1}"/> <see cref="JobType"/> or <see cref="None"/>.</returns>
    Result<JobType, GetForError> GetFor(Type type);

    /// <summary>
    /// Gets the job <see cref="Type"/> associated with the <see cref="JobType"/>.
    /// </summary>
    /// <param name="type">The <see cref="JobType"/>.</param>
    /// <returns><see cref="Result{T0, T1}"/> <see cref="Type"/> or <see cref="GetClrTypeForError"/>.</returns>
    Result<Type, GetClrTypeForError> GetClrTypeFor(JobType type);

    /// <summary>
    /// Gets the job request <see cref="Type"/> associated with the <see cref="JobType"/>.
    /// </summary>
    /// <param name="type">The <see cref="JobType"/>.</param>
    /// <returns><see cref="Result{T0, T1}"/> <see cref="Type"/> or <see cref="GetRequestClrTypeForError"/>.</returns>
    Result<Type, GetRequestClrTypeForError> GetRequestClrTypeFor(JobType type);
}
