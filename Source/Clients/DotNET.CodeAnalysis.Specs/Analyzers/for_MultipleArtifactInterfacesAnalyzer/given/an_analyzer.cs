// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_MultipleArtifactInterfacesAnalyzer.given;

public class an_analyzer : Specification
{
    protected static string CreateSource(string usage) => """
        namespace Cratis.Chronicle.Projections
        {
            public interface IProjectionFor<T> { }
        }
        namespace Cratis.Chronicle.Reducers
        {
            public interface IReducerFor<T> { }
        }
        namespace Sample
        {
            public class First { }
            public class Second { }
        """ + usage + "}";
}
