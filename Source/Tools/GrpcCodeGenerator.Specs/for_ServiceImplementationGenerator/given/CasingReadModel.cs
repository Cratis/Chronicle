// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceImplementationGenerator.given;

[ReadModel]
public record CasingReadModel(string Name)
{
    public static CasingReadModel ForNamespace(string eventStore, string @namespace) => new(@namespace);

    public static Task<CasingReadModel> ForApplication(string eventStore, string @namespace, bool applicationScope) => Task.FromResult(new CasingReadModel(eventStore));

    public static Task<CasingReadModel> ForStore(string eventStore) => Task.FromResult(new CasingReadModel(eventStore));

    public static IObservable<CasingReadModel> ObserveNamespace(string eventStore, string @namespace) => Observable.Return(new CasingReadModel(@namespace));
}
