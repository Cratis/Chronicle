// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>Thrown when a unit of work implementation cannot enroll decision reads.</summary>
/// <param name="unitOfWorkType">The unsupported unit of work type.</param>
public class UnitOfWorkDecisionReadsNotSupported(Type unitOfWorkType)
    : Exception($"Unit of work '{unitOfWorkType}' does not support decision reads.");
