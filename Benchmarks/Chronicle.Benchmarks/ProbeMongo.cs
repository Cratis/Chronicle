// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using DotNet.Testcontainers.Containers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Benchmarks;

internal sealed class ProbeMongo(IContainer container, string store)
{
    const string Comment = "probe-4748-instrumentation";
    readonly MongoClient _client = new($"mongodb://localhost:{Environment.GetEnvironmentVariable("BENCH_MONGO_PORT") ?? "27018"}/?directConnection=true&serverSelectionTimeoutMS=5000");
    readonly string[] _databases = [store, $"{store}+es", $"{store}+es+Default"];

    internal async Task<JsonElement> Script(string script)
    {
        var result = await container.ExecAsync(["mongosh", "--quiet", "--eval", script]);
        if (result.ExitCode != 0) throw new InvalidOperationException($"Mongo measurement failed: {result.Stderr} {result.Stdout}");
        return JsonDocument.Parse(result.Stdout.Trim().Split('\n').Last()).RootElement.Clone();
    }

    string _probeId = "";
    JsonElement _transactionStart;

    internal async Task<JsonElement> StartProfile(string probeId)
    {
        _probeId = probeId;
        var start = await Script($$$$$"""
        const names = {{{{{JsonSerializer.Serialize(_databases)}}}}};
        for (const name of names) {
          const d = db.getSiblingDB(name);
          d.createCollection('system.profile', {capped:true, size:67108864});
          const result=d.runCommand({profile:2,filter:{'command.comment':{$ne:'probe-4748-instrumentation'}, 'originatingCommand.comment':{$ne:'probe-4748-instrumentation'}}});
          if(result.ok!==1) throw new Error('Cannot enable scoped profiler: '+JSON.stringify(result));
          d.runCommand({find:'probe-boundary',filter:{},comment:'probe-4748-boundary'});
        }
        const admin=db.getSiblingDB('admin');
        const adminResult=admin.runCommand({profile:2,filter:{$or:[{'command.commitTransaction':{$exists:true}},{'command.abortTransaction':{$exists:true}}]}});
        if(adminResult.ok!==1) throw new Error('Cannot enable transaction profiler: '+JSON.stringify(adminResult));
        const counters=admin.serverStatus().transactions;
        const serverTransactions={totalCommitted:Number(counters.totalCommitted),totalAborted:Number(counters.totalAborted)};
        if(!Object.values(serverTransactions).every(n=>Number.isSafeInteger(n)&&n>=0)) throw new Error('Invalid starting transaction counters');
        print(JSON.stringify({databases:names,serverTransactions,profiling:names.map(name=>({database:name,status:db.getSiblingDB(name).getProfilingStatus()})),adminStatus:admin.getProfilingStatus(),method:'database-scoped profiler; transaction commands in isolated container admin profiler'}));
        """);
        _transactionStart = start.GetProperty("serverTransactions").Clone();
        return start;
    }

    internal Task<JsonElement> EndProfile() => Script($$$$$"""
        const names = {{{{{JsonSerializer.Serialize(_databases)}}}}};
        const serverStart={{{{{_transactionStart.GetRawText()}}}}};
        const counters=db.getSiblingDB('admin').serverStatus().transactions;
        const serverEnd={totalCommitted:Number(counters.totalCommitted),totalAborted:Number(counters.totalAborted)};
        if(!Object.values(serverEnd).every(n=>Number.isSafeInteger(n)&&n>=0)) throw new Error('Invalid ending transaction counters');
        const serverCommittedDelta=serverEnd.totalCommitted-serverStart.totalCommitted;
        const serverAbortedDelta=serverEnd.totalAborted-serverStart.totalAborted;
        const rows=[]; const boundary=[]; const samples=[];
        let profileEntries=0, unparsed=0, transactionIdentityMissing=0;
        const transactionOwners=new Map(), jobOwners=new Map(), observedTransactions=new Set();
        function transactionKey(r) {
          const c=r.command||{};
          const session=c.lsid??r.lsid, number=c.txnNumber??r.txnNumber;
          return session?.id!==undefined && number!==undefined && number!==null ? EJSON.stringify([session.id,number]) : null;
        }
        const probeId = {{{{{JsonSerializer.Serialize(_probeId)}}}}};
        const systemIds = {{{{{JsonSerializer.Serialize(Probe.SystemObserverIds)}}}}};
        const knownIds = [probeId,...systemIds];
        function idsIn(value) {
          if (typeof value==='string') return knownIds.filter(id=>value===id || value.startsWith(id+'+'));
          if (!value || typeof value!=='object') return [];
          return [...new Set(Object.values(value).flatMap(idsIn))];
        }
        function array(value) {
          if (value===undefined || value===null) return [];
          if (!Array.isArray(value)) throw new TypeError('Expected command batch array');
          return value;
        }
        function commandOf(r) {
          const c=r.command??{};
          if (typeof c!=='object' || Array.isArray(c)) throw new TypeError('Expected command object');
          return c;
        }
        function metric(value) {
          const n=Number(value??0);
          if (!Number.isFinite(n) || n<0) throw new TypeError('Invalid profiler metric');
          return n;
        }
        function attribution(r) {
          const c=commandOf(r);
          const selectors=[r.query,c.filter,c.q,c.query,c.pipeline,c.document?c.document._id:null,
            ...array(c.updates).map(u=>u.q),...array(c.deletes).map(u=>u.q),...array(c.documents).map(d=>d._id)];
          if (/\.(jobs|job-steps)$/.test(r.ns)) {
            selectors.push(c.u);
            const key=c.q?._id===undefined ? null : EJSON.stringify([r.ns,c.q._id]);
            const declared=idsIn(c.u);
            if (key && declared.length===1) jobOwners.set(key,declared[0]);
            if (key && jobOwners.has(key)) selectors.push(jobOwners.get(key));
          }
          const ids=[...new Set(selectors.flatMap(idsIn))];
          return ids.length===1 ? [ids[0]===probeId?'probe':'system',ids[0]] : ['shared',''];
        }
        for (const name of names) {
          const d=db.getSiblingDB(name); d.setProfilingLevel(0);
          boundary.push({database:name,retained:d.system.profile.countDocuments({'command.comment':'probe-4748-boundary'})===1});
          const grouped=new Map();
          // mongosh cursors are not synchronous JS iterables; materialize before for-of.
          for (const r of d.system.profile.find({ns:{$not:/system\.profile/},
            'command.comment':{$nin:['probe-4748-boundary','probe-4748-instrumentation']},
            'originatingCommand.comment':{$ne:'probe-4748-instrumentation'}}).toArray()) {
            profileEntries++;
            try {
              const cmd=Object.keys(commandOf(r))[0]||'';
              const [owner,observerId]=attribution(r);
              const transaction=transactionKey(r);
              const command=commandOf(r);
              // txnNumber alone also labels retryable writes; only autocommit:false denotes a real transaction.
              if (command.autocommit===false || r.autocommit===false || command.startTransaction===true) {
                if (transaction) observedTransactions.add(transaction);
                else transactionIdentityMissing++;
              }
              if (transaction) {
                const owners=transactionOwners.get(transaction)||new Set();
                owners.add(JSON.stringify([owner,observerId]));
                transactionOwners.set(transaction,owners);
              }
              const metrics={documentsExamined:metric(r.docsExamined),documentsReturned:metric(r.nreturned),
                documentsInserted:metric(r.ninserted),documentsMatched:metric(r.nMatched),documentsModified:metric(r.nModified),
                documentsDeleted:metric(r.ndeleted),responseBytes:metric(r.responseLength)};
              const key=JSON.stringify([r.ns,r.op,cmd,owner,observerId]);
              const row=grouped.get(key)||{ns:r.ns,op:r.op,command:cmd,owner,observerId,commands:0};
              row.commands++;
              for (const [field,value] of Object.entries(metrics)) row[field]=(row[field]||0)+value;
              grouped.set(key,row);
              if(samples.length<20) samples.push({ns:r.ns,op:r.op,command:r.command,query:r.query,lsid:r.lsid,txnNumber:r.txnNumber,owner,observerId});
            } catch (_) { unparsed++; }
          }
          rows.push(...grouped.values());
        }
        const admin=db.getSiblingDB('admin'); admin.setProfilingLevel(0);
        const transactions=[];
        for (const r of admin.system.profile.find({$or:[{'command.commitTransaction':{$exists:true}},{'command.abortTransaction':{$exists:true}}]}).toArray()) {
          try {
            const key=transactionKey(r), owners=transactionOwners.get(key);
            const [owner,observerId]=!owners ? ['uncorrelated',''] : owners.size===1 ? JSON.parse([...owners][0]) : ['shared',''];
            transactions.push({op:r.op,command:Object.keys(commandOf(r))[0],ns:r.ns,owner,observerId,
              sessionTransaction:key,responseBytes:metric(r.responseLength),documentsExamined:metric(r.docsExamined)});
          } catch (_) { unparsed++; }
        }
        const adminSamples=admin.system.profile.find().limit(20).toArray().map(r=>({op:r.op,ns:r.ns,command:r.command,lsid:r.lsid,txnNumber:r.txnNumber}));
        print(JSON.stringify({databases:names,boundary,rows,samples,transactions,adminSamples,profileEntries,unparsed,
          transactionSet:[...observedTransactions],transactionsObserved:observedTransactions.size,terminalCommandsProfiled:transactions.length,
          terminalCostProfiled:false,transactionIdentityMissing,serverStart,serverEnd,serverCommittedDelta,serverAbortedDelta,
          transactionCounterMethod:'explicit run-bounded serverStatus().transactions delta; task-owned single-tenant Mongo container; not profiler terminal cost',commands:rows.reduce((n,r)=>n+r.commands,0)+transactions.length}));
        """);

    internal async Task<BsonDocument[]> Observers() => await Read($"{store}+es", "observers");

    internal Task<BsonDocument[]> State() => Read($"{store}+es+Default", "observers");

    internal Task<BsonDocument[]> Jobs() => Read($"{store}+es+Default", "jobs");

    internal async Task<BsonDocument[]> JobWrites()
    {
        using var cursor = await _client.GetDatabase($"{store}+es+Default").GetCollection<BsonDocument>("system.profile")
            .FindAsync(new BsonDocument { { "ns", $"{store}+es+Default.jobs" }, { "op", new BsonDocument("$in", new BsonArray { "update", "remove" }) } },
                new FindOptions<BsonDocument> { Comment = Comment });
        return (await cursor.ToListAsync()).ToArray();
    }

    internal async Task<BsonDocument[]> CompletedCatchupJobs(string observerId, ulong tail)
    {
        var writes = await JobWrites();
        var matching = writes.Where(r => r["op"] == "update" && r["command"]["u"].AsBsonDocument.GetValue("details", "") == observerId)
            .GroupBy(r => r["command"]["q"]["_id"]);
        var completed = new List<BsonDocument>();
        foreach (var group in matching)
        {
            var write = group.Last();
            var job = write["command"]["u"].AsBsonDocument;
            var progress = job["progress"].AsBsonDocument;
            var key = job["request"]["observerKey"].AsBsonDocument;
            var removal = writes.LastOrDefault(r => r["op"] == "remove" && r["command"]["q"]["_id"].Equals(group.Key) &&
                r.GetValue("ndeleted", 0).ToInt32() == 1 && r["ts"].AsBsonDateTime.MillisecondsSinceEpoch >= write["ts"].AsBsonDateTime.MillisecondsSinceEpoch);
            if (job["type"] != "CatchUpObserver" || key["observerId"] != observerId || key["eventStore"] != store ||
                key["namespace"] != "Default" || key["eventSequenceId"] != "event-log") continue;
            if (progress["totalSteps"].ToInt32() <= 0 || progress["successfulSteps"] != progress["totalSteps"] ||
                progress["failedSteps"].ToInt32() != 0 || progress["stoppedSteps"].ToInt32() != 0 || progress["unreachableSteps"].ToInt32() != 0 ||
                (job["request"]["observerType"].ToInt32() != 2 &&
                    (!job["handledAllEvents"].AsBoolean || job["lastHandledEventSequenceNumber"].ToDecimal() < tail))) continue;
            // Successful jobs are deleted without persisting status 5. Require the actual full-success write AND same-id deletion.
            if (job["status"].ToInt32() != 5 && removal is null) continue;
            completed.Add(new BsonDocument { { "job", job }, { "writeTimestamp", write["ts"] },
                { "removalTimestamp", removal?.GetValue("ts") ?? BsonNull.Value },
                { "method", "profiled full-success job replacement plus same-id deletion (or persisted status 5)" } });
        }
        return completed.ToArray();
    }

    internal Dictionary<string, string[]> ProjectionEvidence { get; } = new();

    internal async Task<BsonDocument[]> ProjectionRows()
    {
        var database = _client.GetDatabase(store);
        var names = await database.RunCommandAsync<BsonDocument>(new BsonDocument
        {
            { "listCollections", 1 }, { "nameOnly", true }, { "comment", Comment }, { "cursor", new BsonDocument("batchSize", 1000) }
        });
        if (names["cursor"]["id"].ToInt64() != 0) throw new InvalidOperationException("Projection collection enumeration was truncated.");
        var collections = names["cursor"]["firstBatch"].AsBsonArray.Select(n => n["name"].AsString).Where(n => n != "system.profile").ToArray();
        var rows = new List<BsonDocument>();
        foreach (var name in collections)
        {
            var documents = await Read(store, name);
            ProjectionEvidence[$"{store}.{name}"] = documents.Select(d => d.ToJson()).ToArray();
            rows.AddRange(documents);
        }
        return rows.ToArray();
    }

    async Task<BsonDocument[]> Read(string database, string collection)
    {
        using var cursor = await _client.GetDatabase(database).GetCollection<BsonDocument>(collection)
            .FindAsync(FilterDefinition<BsonDocument>.Empty, new FindOptions<BsonDocument> { Comment = Comment });
        return (await cursor.ToListAsync()).ToArray();
    }
}
