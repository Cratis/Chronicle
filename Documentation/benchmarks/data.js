window.BENCHMARK_DATA = {
  "lastUpdate": 1693908072367,
  "repoUrl": "https://github.com/aksio-insurtech/Cratis",
  "entries": {
    "Cratis Benchmarks": [
      {
        "commit": {
          "author": {
            "name": "Einar Ingebrigtsen",
            "username": "einari",
            "email": "einari@me.com"
          },
          "committer": {
            "name": "Einar Ingebrigtsen",
            "username": "einari",
            "email": "einari@me.com"
          },
          "id": "e35158a4b43872a908e855f8a79e0c3f43bef60a",
          "message": "Actually skipping the fetch for main",
          "timestamp": "2023-09-05T09:50:10Z",
          "url": "https://github.com/aksio-insurtech/Cratis/commit/e35158a4b43872a908e855f8a79e0c3f43bef60a"
        },
        "date": 1693908072248,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Benchmarks.EventSequences.AppendingEvent.Single",
            "value": 25560876.444444444,
            "unit": "ns",
            "range": "± 4472395.393041354"
          },
          {
            "name": "Benchmarks.EventSequences.AppendingEvents.InSequence(NumberOfEvents: 10)",
            "value": 88639068.1,
            "unit": "ns",
            "range": "± 15702776.710705357"
          },
          {
            "name": "Benchmarks.Observation.HandlingEvents.InSequence(NumberOfEvents: 10)",
            "value": 72457803.8888889,
            "unit": "ns",
            "range": "± 8396344.235052144"
          },
          {
            "name": "Benchmarks.Projections.ProjectingEvents.InSequence(NumberOfEvents: 10)",
            "value": 59352870.125,
            "unit": "ns",
            "range": "± 2899337.914425658"
          },
          {
            "name": "Benchmarks.Reducers.ReducingEvents.InSequence(NumberOfEvents: 10)",
            "value": 79365528.8888889,
            "unit": "ns",
            "range": "± 12379035.580170376"
          },
          {
            "name": "Benchmarks.EventSequences.AppendingEvents.WithAppendMany(NumberOfEvents: 10)",
            "value": 82380462.3,
            "unit": "ns",
            "range": "± 9355783.879575351"
          },
          {
            "name": "Benchmarks.EventSequences.AppendingEvents.InSequence(NumberOfEvents: 100)",
            "value": 509460835.2,
            "unit": "ns",
            "range": "± 36224206.5938479"
          },
          {
            "name": "Benchmarks.Observation.HandlingEvents.InSequence(NumberOfEvents: 100)",
            "value": 622063278.8,
            "unit": "ns",
            "range": "± 51697496.9911325"
          },
          {
            "name": "Benchmarks.Projections.ProjectingEvents.InSequence(NumberOfEvents: 100)",
            "value": 487554897.3333333,
            "unit": "ns",
            "range": "± 26769245.3989016"
          },
          {
            "name": "Benchmarks.Reducers.ReducingEvents.InSequence(NumberOfEvents: 100)",
            "value": 601402713.75,
            "unit": "ns",
            "range": "± 10405985.604896646"
          },
          {
            "name": "Benchmarks.EventSequences.AppendingEvents.WithAppendMany(NumberOfEvents: 100)",
            "value": 446248245.2,
            "unit": "ns",
            "range": "± 17568312.204750285"
          },
          {
            "name": "Benchmarks.EventSequences.AppendingEvents.InSequence(NumberOfEvents: 1000)",
            "value": 5126682361.111111,
            "unit": "ns",
            "range": "± 285751661.1800688"
          },
          {
            "name": "Benchmarks.Observation.HandlingEvents.InSequence(NumberOfEvents: 1000)",
            "value": 6765788017.1,
            "unit": "ns",
            "range": "± 581807025.3077468"
          },
          {
            "name": "Benchmarks.Projections.ProjectingEvents.InSequence(NumberOfEvents: 1000)",
            "value": 4784470114.3,
            "unit": "ns",
            "range": "± 207476274.50204736"
          },
          {
            "name": "Benchmarks.Reducers.ReducingEvents.InSequence(NumberOfEvents: 1000)",
            "value": 6704486886.2,
            "unit": "ns",
            "range": "± 333061432.5674476"
          },
          {
            "name": "Benchmarks.EventSequences.AppendingEvents.WithAppendMany(NumberOfEvents: 1000)",
            "value": 4992083077.666667,
            "unit": "ns",
            "range": "± 122662038.7267309"
          }
        ]
      }
    ]
  }
}