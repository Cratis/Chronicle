window.BENCHMARK_DATA = {
  "lastUpdate": 1697970605432,
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
      },
      {
        "commit": {
          "author": {
            "email": "einari@me.com",
            "name": "Einar Ingebrigtsen",
            "username": "einari"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "fd4a70c8f664c3dba9db93762249c933b17a022d",
          "message": "Merge pull request #942 from woksin/initial-partition-failed\n\nInitial partition failed",
          "timestamp": "2023-09-10T08:23:12+02:00",
          "tree_id": "574cce8e20f7b13de2a1db9ddf57f80acad127ef",
          "url": "https://github.com/aksio-insurtech/Cratis/commit/fd4a70c8f664c3dba9db93762249c933b17a022d"
        },
        "date": 1694327408560,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Benchmarks.EventSequences.AppendingEvent.Single",
            "value": 20362490.6,
            "unit": "ns",
            "range": "± 1809958.9981574598"
          },
          {
            "name": "Benchmarks.EventSequences.AppendingEvents.InSequence(NumberOfEvents: 10)",
            "value": 68537126.3,
            "unit": "ns",
            "range": "± 5833140.723908139"
          },
          {
            "name": "Benchmarks.Observation.HandlingEvents.InSequence(NumberOfEvents: 10)",
            "value": 60165989.4,
            "unit": "ns",
            "range": "± 5623923.147615949"
          },
          {
            "name": "Benchmarks.Projections.ProjectingEvents.InSequence(NumberOfEvents: 10)",
            "value": 42209565.777777776,
            "unit": "ns",
            "range": "± 2360617.497908745"
          },
          {
            "name": "Benchmarks.Reducers.ReducingEvents.InSequence(NumberOfEvents: 10)",
            "value": 51611053,
            "unit": "ns",
            "range": "± 2568020.994937368"
          },
          {
            "name": "Benchmarks.EventSequences.AppendingEvents.WithAppendMany(NumberOfEvents: 10)",
            "value": 60180574,
            "unit": "ns",
            "range": "± 3421254.8649263196"
          },
          {
            "name": "Benchmarks.EventSequences.AppendingEvents.InSequence(NumberOfEvents: 100)",
            "value": 421096374.2,
            "unit": "ns",
            "range": "± 16838596.499308895"
          },
          {
            "name": "Benchmarks.Observation.HandlingEvents.InSequence(NumberOfEvents: 100)",
            "value": 476240784.1,
            "unit": "ns",
            "range": "± 14354626.905333938"
          },
          {
            "name": "Benchmarks.Projections.ProjectingEvents.InSequence(NumberOfEvents: 100)",
            "value": 340392808.2,
            "unit": "ns",
            "range": "± 13424308.82200803"
          },
          {
            "name": "Benchmarks.Reducers.ReducingEvents.InSequence(NumberOfEvents: 100)",
            "value": 430311094.25,
            "unit": "ns",
            "range": "± 4458395.080864309"
          },
          {
            "name": "Benchmarks.EventSequences.AppendingEvents.WithAppendMany(NumberOfEvents: 100)",
            "value": 384789666,
            "unit": "ns",
            "range": "± 14680602.467142401"
          },
          {
            "name": "Benchmarks.EventSequences.AppendingEvents.InSequence(NumberOfEvents: 1000)",
            "value": 4657082775.4,
            "unit": "ns",
            "range": "± 687271748.7856202"
          },
          {
            "name": "Benchmarks.Observation.HandlingEvents.InSequence(NumberOfEvents: 1000)",
            "value": 4483060318.2,
            "unit": "ns",
            "range": "± 22922937.356836855"
          },
          {
            "name": "Benchmarks.Projections.ProjectingEvents.InSequence(NumberOfEvents: 1000)",
            "value": 3253402579.2,
            "unit": "ns",
            "range": "± 11145721.47812324"
          },
          {
            "name": "Benchmarks.Reducers.ReducingEvents.InSequence(NumberOfEvents: 1000)",
            "value": 4166376468.75,
            "unit": "ns",
            "range": "± 10107609.009267623"
          },
          {
            "name": "Benchmarks.EventSequences.AppendingEvents.WithAppendMany(NumberOfEvents: 1000)",
            "value": 4205039171.9,
            "unit": "ns",
            "range": "± 130788282.6165374"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Einar Ingebrigtsen",
            "username": "einari",
            "email": "einari@me.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "cc9845a94cdfa8b23d6db8a22e5bf8c04fc7ca91",
          "message": "Update README.md",
          "timestamp": "2023-09-18T12:01:05Z",
          "url": "https://github.com/aksio-insurtech/Cratis/commit/cc9845a94cdfa8b23d6db8a22e5bf8c04fc7ca91"
        },
        "date": 1695039165066,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Benchmarks.EventSequences.AppendingEvent.Single",
            "value": 26857917.7,
            "unit": "ns",
            "range": "± 3134306.8873480377"
          },
          {
            "name": "Benchmarks.EventSequences.AppendingEvents.InSequence(NumberOfEvents: 10)",
            "value": 107198210.3,
            "unit": "ns",
            "range": "± 19446135.436243035"
          },
          {
            "name": "Benchmarks.Observation.HandlingEvents.InSequence(NumberOfEvents: 10)",
            "value": 101427347.5,
            "unit": "ns",
            "range": "± 6633378.692295008"
          },
          {
            "name": "Benchmarks.Projections.ProjectingEvents.InSequence(NumberOfEvents: 10)",
            "value": 54958919.11111111,
            "unit": "ns",
            "range": "± 1577104.6924244158"
          },
          {
            "name": "Benchmarks.Reducers.ReducingEvents.InSequence(NumberOfEvents: 10)",
            "value": 80552848.5,
            "unit": "ns",
            "range": "± 4974178.87649615"
          },
          {
            "name": "Benchmarks.EventSequences.AppendingEvents.WithAppendMany(NumberOfEvents: 10)",
            "value": 86518615.7,
            "unit": "ns",
            "range": "± 12651133.330599718"
          },
          {
            "name": "Benchmarks.EventSequences.AppendingEvents.InSequence(NumberOfEvents: 100)",
            "value": 690074229.3,
            "unit": "ns",
            "range": "± 46532185.479161814"
          },
          {
            "name": "Benchmarks.Observation.HandlingEvents.InSequence(NumberOfEvents: 100)",
            "value": 893806452.4,
            "unit": "ns",
            "range": "± 42182771.78693453"
          },
          {
            "name": "Benchmarks.Projections.ProjectingEvents.InSequence(NumberOfEvents: 100)",
            "value": 477157436.1,
            "unit": "ns",
            "range": "± 24488959.58972021"
          },
          {
            "name": "Benchmarks.Reducers.ReducingEvents.InSequence(NumberOfEvents: 100)",
            "value": 622683927.7,
            "unit": "ns",
            "range": "± 33713668.98157771"
          },
          {
            "name": "Benchmarks.EventSequences.AppendingEvents.WithAppendMany(NumberOfEvents: 100)",
            "value": 571739471.2,
            "unit": "ns",
            "range": "± 35397539.98657071"
          },
          {
            "name": "Benchmarks.EventSequences.AppendingEvents.InSequence(NumberOfEvents: 1000)",
            "value": 6007301520,
            "unit": "ns",
            "range": "± 622912731.5651591"
          },
          {
            "name": "Benchmarks.Observation.HandlingEvents.InSequence(NumberOfEvents: 1000)",
            "value": 7530780371.8,
            "unit": "ns",
            "range": "± 803400669.084285"
          },
          {
            "name": "Benchmarks.Projections.ProjectingEvents.InSequence(NumberOfEvents: 1000)",
            "value": 4762592543.6,
            "unit": "ns",
            "range": "± 69534956.48236701"
          },
          {
            "name": "Benchmarks.Reducers.ReducingEvents.InSequence(NumberOfEvents: 1000)",
            "value": 5912686954.777778,
            "unit": "ns",
            "range": "± 200459116.7006325"
          },
          {
            "name": "Benchmarks.EventSequences.AppendingEvents.WithAppendMany(NumberOfEvents: 1000)",
            "value": 5531212677.3,
            "unit": "ns",
            "range": "± 646818456.6425856"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "einari@me.com",
            "name": "Einar Ingebrigtsen",
            "username": "einari"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "a52634d4b467aba99af52f38eb8188d0075e5f28",
          "message": "Merge pull request #968 from aksio-insurtech:fix/graceful-identities\n\nFallback to NotSet or Unknown for identity, instead of throwing an exception",
          "timestamp": "2023-09-25T15:07:46+02:00",
          "tree_id": "b596bc57c3e3828043a9b7db1b03bafe8090c567",
          "url": "https://github.com/aksio-insurtech/Cratis/commit/a52634d4b467aba99af52f38eb8188d0075e5f28"
        },
        "date": 1695647729070,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Benchmarks.EventSequences.AppendingEvent.Single",
            "value": 20924644.1,
            "unit": "ns",
            "range": "± 2809919.8076816457"
          },
          {
            "name": "Benchmarks.EventSequences.AppendingEvents.InSequence(NumberOfEvents: 10)",
            "value": 65103813.4,
            "unit": "ns",
            "range": "± 6591435.709154927"
          },
          {
            "name": "Benchmarks.Observation.HandlingEvents.InSequence(NumberOfEvents: 10)",
            "value": 73563746.8,
            "unit": "ns",
            "range": "± 9946197.628540251"
          },
          {
            "name": "Benchmarks.Projections.ProjectingEvents.InSequence(NumberOfEvents: 10)",
            "value": 44130030.8,
            "unit": "ns",
            "range": "± 2042033.8632387398"
          },
          {
            "name": "Benchmarks.Reducers.ReducingEvents.InSequence(NumberOfEvents: 10)",
            "value": 51690487.2,
            "unit": "ns",
            "range": "± 1610209.476556996"
          },
          {
            "name": "Benchmarks.EventSequences.AppendingEvents.WithAppendMany(NumberOfEvents: 10)",
            "value": 63799769.7,
            "unit": "ns",
            "range": "± 7887825.347809018"
          },
          {
            "name": "Benchmarks.EventSequences.AppendingEvents.InSequence(NumberOfEvents: 100)",
            "value": 466692631.1,
            "unit": "ns",
            "range": "± 14159980.126204144"
          },
          {
            "name": "Benchmarks.Observation.HandlingEvents.InSequence(NumberOfEvents: 100)",
            "value": 608328414.4,
            "unit": "ns",
            "range": "± 14556508.794197423"
          },
          {
            "name": "Benchmarks.Projections.ProjectingEvents.InSequence(NumberOfEvents: 100)",
            "value": 344379082.3,
            "unit": "ns",
            "range": "± 10805363.479631934"
          },
          {
            "name": "Benchmarks.Reducers.ReducingEvents.InSequence(NumberOfEvents: 100)",
            "value": 445913085.3,
            "unit": "ns",
            "range": "± 11684250.446994906"
          },
          {
            "name": "Benchmarks.EventSequences.AppendingEvents.WithAppendMany(NumberOfEvents: 100)",
            "value": 435552030.8,
            "unit": "ns",
            "range": "± 22258030.822238877"
          },
          {
            "name": "Benchmarks.EventSequences.AppendingEvents.InSequence(NumberOfEvents: 1000)",
            "value": 4221602827.125,
            "unit": "ns",
            "range": "± 46275391.5812317"
          },
          {
            "name": "Benchmarks.Observation.HandlingEvents.InSequence(NumberOfEvents: 1000)",
            "value": 5490077434.9,
            "unit": "ns",
            "range": "± 593754386.4014739"
          },
          {
            "name": "Benchmarks.Projections.ProjectingEvents.InSequence(NumberOfEvents: 1000)",
            "value": 3247803040.285714,
            "unit": "ns",
            "range": "± 25076380.846968636"
          },
          {
            "name": "Benchmarks.Reducers.ReducingEvents.InSequence(NumberOfEvents: 1000)",
            "value": 4196972670.5,
            "unit": "ns",
            "range": "± 2511719.8410950876"
          },
          {
            "name": "Benchmarks.EventSequences.AppendingEvents.WithAppendMany(NumberOfEvents: 1000)",
            "value": 3718193630.3,
            "unit": "ns",
            "range": "± 374808504.9854404"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "einari@me.com",
            "name": "Einar Ingebrigtsen",
            "username": "einari"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "5166ac6e99b3ebfb664acb5382b0a9efd1d94bb2",
          "message": "Merge pull request #994 from aksio-insurtech:fix/workers\n\nChanging stop to do nothing if there is nothing to stop. + do not write its state - it can be stale",
          "timestamp": "2023-10-22T12:22:38+02:00",
          "tree_id": "9e4f03feae766b6c953e6c2158a231edc0d613b6",
          "url": "https://github.com/aksio-insurtech/Cratis/commit/5166ac6e99b3ebfb664acb5382b0a9efd1d94bb2"
        },
        "date": 1697970605344,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Benchmarks.EventSequences.AppendingEvent.Single",
            "value": 22154359.444444444,
            "unit": "ns",
            "range": "± 2905591.4580877554"
          },
          {
            "name": "Benchmarks.EventSequences.AppendingEvents.InSequence(NumberOfEvents: 10)",
            "value": 76034346.7,
            "unit": "ns",
            "range": "± 8552142.067818409"
          },
          {
            "name": "Benchmarks.Observation.HandlingEvents.InSequence(NumberOfEvents: 10)",
            "value": 71738223.1,
            "unit": "ns",
            "range": "± 8260100.120936543"
          },
          {
            "name": "Benchmarks.Projections.ProjectingEvents.InSequence(NumberOfEvents: 10)",
            "value": 41343648,
            "unit": "ns",
            "range": "± 976313.8035010824"
          },
          {
            "name": "Benchmarks.Reducers.ReducingEvents.InSequence(NumberOfEvents: 10)",
            "value": 51817718.9,
            "unit": "ns",
            "range": "± 1231013.748792113"
          },
          {
            "name": "Benchmarks.EventSequences.AppendingEvents.WithAppendMany(NumberOfEvents: 10)",
            "value": 67945567.1111111,
            "unit": "ns",
            "range": "± 3038081.495075363"
          },
          {
            "name": "Benchmarks.EventSequences.AppendingEvents.InSequence(NumberOfEvents: 100)",
            "value": 503245889.3333333,
            "unit": "ns",
            "range": "± 6712961.275047291"
          },
          {
            "name": "Benchmarks.Observation.HandlingEvents.InSequence(NumberOfEvents: 100)",
            "value": 593212422.6,
            "unit": "ns",
            "range": "± 19274266.27307481"
          },
          {
            "name": "Benchmarks.Projections.ProjectingEvents.InSequence(NumberOfEvents: 100)",
            "value": 340070842.5,
            "unit": "ns",
            "range": "± 9634514.712673126"
          },
          {
            "name": "Benchmarks.Reducers.ReducingEvents.InSequence(NumberOfEvents: 100)",
            "value": 428335348.8333333,
            "unit": "ns",
            "range": "± 2021634.6754427634"
          },
          {
            "name": "Benchmarks.EventSequences.AppendingEvents.WithAppendMany(NumberOfEvents: 100)",
            "value": 397584324,
            "unit": "ns",
            "range": "± 7638372.630575327"
          },
          {
            "name": "Benchmarks.EventSequences.AppendingEvents.InSequence(NumberOfEvents: 1000)",
            "value": 4044243299.7,
            "unit": "ns",
            "range": "± 196994044.79086125"
          },
          {
            "name": "Benchmarks.Observation.HandlingEvents.InSequence(NumberOfEvents: 1000)",
            "value": 4496261747.9,
            "unit": "ns",
            "range": "± 110419327.72691178"
          },
          {
            "name": "Benchmarks.Projections.ProjectingEvents.InSequence(NumberOfEvents: 1000)",
            "value": 3217421738.6,
            "unit": "ns",
            "range": "± 42780172.1628322"
          },
          {
            "name": "Benchmarks.Reducers.ReducingEvents.InSequence(NumberOfEvents: 1000)",
            "value": 4136311869,
            "unit": "ns",
            "range": "± 6821916.3006533235"
          },
          {
            "name": "Benchmarks.EventSequences.AppendingEvents.WithAppendMany(NumberOfEvents: 1000)",
            "value": 4036241996.428571,
            "unit": "ns",
            "range": "± 32411769.54228108"
          }
        ]
      }
    ]
  }
}