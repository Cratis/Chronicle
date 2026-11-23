// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Namespaces;

namespace Cratis.Chronicle.Sequences.for_AppendToNamespace;

public class when_the_feature_is_unavailable : given.a_protected_append_endpoint
{
    void Establish() => _incarnations.GetStorage("store").Returns(_ => throw new NamespaceIncarnationUnavailable((EventStoreName)"store"));

    Task Because() => Append();

    [Fact] void should_refuse_the_append() => _error.ShouldBeOfExactType<NamespaceIncarnationUnavailable>();
    [Fact] void should_not_fall_back_to_an_unfenced_append() => _eventSequence.ReceivedCalls().ShouldBeEmpty();
}
