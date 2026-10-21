// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Kernel.Integration.Occurrences;

[CollectionDefinition(Name, DisableParallelization = true)]
public class SqlOccurrenceCollection : ICollectionFixture<SqlOccurrenceFixture>
{
    public const string Name = "SQL occurrence crash integration";
}
