// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// The exception that is thrown when a MongoDB spec container does not become ready within its start deadline.
/// </summary>
/// <param name="deadline">The deadline the container had to become ready within.</param>
/// <param name="logs">The tail of the container's logs, or a note on why they could not be read.</param>
/// <param name="innerException">The cancellation that ended the start.</param>
public class MongoDBContainerStartTimedOut(TimeSpan deadline, string logs, Exception innerException) : Exception(
    $"The MongoDB spec container did not become ready within {deadline.TotalSeconds:0} seconds, so the fixture failed instead of hanging the test run. Container logs:{Environment.NewLine}{logs}",
    innerException);
