// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs.for_GraphViewReader.given;

public class a_rejected_view : a_graph_view_reader
{
    internal GraphViews _views;

    internal InvalidGraphView FailureOf<TView>() => (InvalidGraphView)_views.Failures[typeof(TView)];
}
