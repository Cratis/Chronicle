// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ReadModelReactors;

public class ReducerClientArtifacts(bool active) : DefaultClientArtifactsProvider(new NoAssemblies())
{
    public override IEnumerable<Type> EventTypes => [typeof(Reduction)];
    public override IEnumerable<Type> Reducers => [active ? typeof(WatchedReducer<ReducedModel>) : typeof(InactiveWatchedReducer<ReducedModel>)];
    public override IEnumerable<Type> ReadModelReactors => [active ? typeof(ReducerRecordingReactor<ReducedModel>) : typeof(InactiveRecordingReactor<ReducedModel>)];

    sealed class NoAssemblies : ICanProvideAssembliesForDiscovery
    {
        public IEnumerable<System.Reflection.Assembly> Assemblies => [];
        public IEnumerable<Type> DefinedTypes => [];
        public void Initialize() { }
    }
}
