// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.for_ProjectionNaming.when_naming;

public class a_qualified_projection : Specification
{
    string _result = null!;

    void Because() => _result = ProjectionNaming.TypeNameFor("Samples.Backoffice.EmployeeList", "Employee");

    [Fact] void should_use_only_the_last_segment() => _result.ShouldEqual("EmployeeList");
}
