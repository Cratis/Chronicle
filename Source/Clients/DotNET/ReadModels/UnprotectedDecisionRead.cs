// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>Thrown when an unprotected read is enrolled as a dependency.</summary>
public class UnprotectedDecisionRead() : Exception("An unprotected read cannot be used as a decision guard.");
