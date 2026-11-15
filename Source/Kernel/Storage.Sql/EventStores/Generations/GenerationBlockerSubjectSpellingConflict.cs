// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Generations;

/// <summary>The exception thrown when a generation initialization blocker cannot be recorded because the store already holds a differently spelled subject for the same kind.</summary>
public class GenerationBlockerSubjectSpellingConflict() : Exception("A generation initialization blocker of this kind is already recorded under a differently spelled subject. No data was changed.");
