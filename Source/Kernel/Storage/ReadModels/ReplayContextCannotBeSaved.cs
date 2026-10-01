// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.ReadModels;

/// <summary>
/// The exception that is thrown when a replay-context provider cannot persist an explicit replay identity.
/// </summary>
public class ReplayContextCannotBeSaved() : Exception("The replay-context provider must support saving an explicit identity before isolated reducer replay can start");
