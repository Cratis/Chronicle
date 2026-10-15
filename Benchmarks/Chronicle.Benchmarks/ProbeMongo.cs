// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using DotNet.Testcontainers.Containers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Benchmarks;

internal sealed class ProbeMongo(IContainer container, string store, long profilerBytes)
{
    const string Comment = "probe-4748-instrumentation";
    readonly MongoClient _client = new($"mongodb://localhost:{Environment.GetEnvironmentVariable("BENCH_MONGO_PORT") ?? "27018"}/?directConnection=true&serverSelectionTimeoutMS=5000");
    readonly string[] _databases = [store, $"{store}+es", $"{store}+es+Default"];
    string _probeId = "";
    JsonElement _transactionStart;
    Task<BsonDocument[]>? _jobWrites;

    internal string[] JobWriteSamples { get; private set; } = [];
    internal Dictionary<string, string[]> ProjectionEvidence { get; } = new();

    internal async Task<JsonElement> Script(string script)
    {
        var result = await container.ExecAsync(["mongosh", "--quiet", "--eval", script]);
        if (result.ExitCode != 0) throw new InvalidOperationException($"Mongo measurement failed: {result.Stderr} {result.Stdout}");
        return JsonDocument.Parse(result.Stdout.Trim().Split('\n').Last()).RootElement.Clone();
    }

    internal async Task<JsonElement> StartProfile(string probeId)
    {
        _probeId = probeId;
        var disk = await container.ExecAsync(["df", "-Pk", "/data/db"]);
        if (disk.ExitCode != 0) throw new InvalidOperationException($"Cannot check owned Mongo disk: {disk.Stderr}");
        var available = long.Parse(disk.Stdout.Trim().Split('\n').Last().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)[3]) * 1024;
        if (available < profilerBytes * 4 + 2147483648L) throw new InvalidOperationException("Insufficient owned Mongo disk for four profiler caps plus 2 GiB headroom.");
        var start = await Script($$$$$"""
        const names={{{{{JsonSerializer.Serialize(_databases)}}}}}, cap={{{{{profilerBytes}}}}};
        const admin=db.getSiblingDB('admin'); const all=[...names,'admin'];
        const capacities=[];
        for(const name of all) {
          const d=db.getSiblingDB(name); d.setProfilingLevel(0);
          const created=d.createCollection('system.profile',{capped:true,size:cap});
          if(created.ok!==1 && created.code!==48) throw new Error('Cannot create profiler: '+JSON.stringify(created));
          if(created.code===48) { const resized=d.runCommand({collMod:'system.profile',cappedSize:cap}); if(resized.ok!==1) throw new Error(JSON.stringify(resized)); }
          const stats=d.runCommand({collStats:'system.profile'});
          if(stats.ok!==1 || !stats.capped || Number(stats.maxSize)<cap) throw new Error('Profiler cap not established');
          capacities.push({database:name,maxSize:Number(stats.maxSize)});
          const filter=name==='admin' ? {$or:[{'command.commitTransaction':{$exists:true}},{'command.abortTransaction':{$exists:true}},{'command.comment':{$in:['probe-4748-boundary','probe-4748-registration']}}]}
            : {'command.comment':{$ne:'probe-4748-instrumentation'},'originatingCommand.comment':{$ne:'probe-4748-instrumentation'}};
          const enabled=d.runCommand({profile:2,filter}); if(enabled.ok!==1) throw new Error(JSON.stringify(enabled));
          d.runCommand({find:'probe-boundary',filter:{},comment:'probe-4748-boundary'});
        }
        const counters=admin.serverStatus().transactions;
        const serverTransactions={totalCommitted:Number(counters.totalCommitted),totalAborted:Number(counters.totalAborted)};
        if(!Object.values(serverTransactions).every(n=>Number.isSafeInteger(n)&&n>=0)) throw new Error('Invalid starting transaction counters');
        print(JSON.stringify({databases:names,capacities,availableBytes:{{{{{available}}}}},serverTransactions,
          profiling:all.map(name=>({database:name,status:db.getSiblingDB(name).getProfilingStatus()})),
          method:'run-scoped profiler, server-side group; explicit single-tenant transaction counter cross-check'}));
        """);
        _transactionStart = start.GetProperty("serverTransactions").Clone();
        return start;
    }

    internal async Task MarkRegistered()
    {
        foreach (var name in _databases.Append("admin"))
            await _client.GetDatabase(name).RunCommandAsync<BsonDocument>(new BsonDocument
            {
                { "find", "probe-boundary" }, { "filter", new BsonDocument() }, { "comment", "probe-4748-registration" }
            });
    }

    internal Task<JsonElement> EndProfile() => Script($$$$$"""
        const names={{{{{JsonSerializer.Serialize(_databases)}}}}}, all=[...names,'admin'];
        const serverStart={{{{{_transactionStart.GetRawText()}}}}};
        const counters=db.getSiblingDB('admin').serverStatus().transactions;
        const serverEnd={totalCommitted:Number(counters.totalCommitted),totalAborted:Number(counters.totalAborted)};
        if(!Object.values(serverEnd).every(n=>Number.isSafeInteger(n)&&n>=0)) throw new Error('Invalid ending transaction counters');
        for(const name of all) db.getSiblingDB(name).setProfilingLevel(0);
        const serverCommittedDelta=serverEnd.totalCommitted-serverStart.totalCommitted, serverAbortedDelta=serverEnd.totalAborted-serverStart.totalAborted;
        const knownIds={{{{{JsonSerializer.Serialize(Probe.SystemObserverIds.Prepend(_probeId).ToArray())}}}}};
        const probeId={{{{{JsonSerializer.Serialize(_probeId)}}}}};
        const rows=[],boundary=[],samples=[],systemOwnerSamples=[],observed=new Set(),owners=new Map();
        let profileEntries=0,transactionIdentityMissing=0;
        function ownerOf(r,ids,probe) {
          function find(v) {
            if(typeof v==='string') return ids.filter(id=>v===id || v.indexOf(id+'+')===0);
            if(!v || typeof v!=='object') return [];
            return Object.values(v).flatMap(find);
          }
          const c=r.command||{};
          const selectors=[r.query,c.filter,c.q,c.query,c.pipeline,c.document?c.document._id:null,
            ...(c.updates||[]).map(u=>u.q),...(c.deletes||[]).map(u=>u.q),...(c.documents||[]).map(d=>d._id)];
          if(/\.(jobs|job-steps)$/.test(r.ns)) selectors.push(c.u);
          const found=[...new Set(selectors.flatMap(find))];
          return {owner:found.length===1?(found[0]===probe?'probe':'system'):'shared',observerId:found.length===1?found[0]:'',
            command:Object.keys(c)[0]||'',isTx:c.autocommit===false || r.autocommit===false || c.startTransaction===true};
        }
        for(const name of all) {
          const d=db.getSiblingDB(name), p=d.getCollection('system.profile');
          const start=p.findOne({'command.comment':'probe-4748-boundary'}), registered=p.findOne({'command.comment':'probe-4748-registration'});
          boundary.push({database:name,retained:!!start,registeredRetained:!!registered,start:start?.ts,registered:registered?.ts});
          if(!start || !registered) continue;
          if(name==='admin') continue;
          const filter={ns:{$not:/system\.profile/},ts:{$gte:start.ts},
            'command.comment':{$nin:['probe-4748-boundary','probe-4748-registration','probe-4748-instrumentation']},
            'originatingCommand.comment':{$ne:'probe-4748-instrumentation'}};
          const sums={};
          const fields={documentsExamined:'docsExamined',documentsReturned:'nreturned',documentsInserted:'ninserted',documentsMatched:'nMatched',documentsModified:'nModified',documentsDeleted:'ndeleted',responseBytes:'responseLength'};
          for(const [field,source] of Object.entries(fields)) sums[field]={$sum:{$ifNull:['$'+source,0]}};
          // All per-operation work and grouping occur on Mongo; mongosh receives only compact groups and bounded samples.
          const grouped=p.aggregate([
            {$match:filter},
            {$set:{meta:{$function:{body:ownerOf,args:['$$ROOT',{$literal:knownIds},{$literal:probeId}],lang:'js'}},session:{$ifNull:['$command.lsid.id','$lsid.id']},number:{$ifNull:['$command.txnNumber','$txnNumber']}}},
            {$group:{_id:{ns:'$ns',op:'$op',command:'$meta.command',owner:'$meta.owner',observerId:'$meta.observerId',
              phase:{$cond:[{$lt:['$ts',registered.ts]},'registration','delivery']},
              transaction:{$cond:['$meta.isTx',{session:'$session',number:'$number'},null]},
              missing:{$and:['$meta.isTx',{$or:[{$eq:[{$ifNull:['$session',null]},null]},{$eq:[{$ifNull:['$number',null]},null]}]}]}},commands:{$sum:1},...sums}}
          ],{allowDiskUse:true,comment:'probe-4748-instrumentation'}).toArray();
          for(const g of grouped) {
            const row={...g._id,commands:Number(g.commands)};
            profileEntries+=row.commands;
            if(row.missing) transactionIdentityMissing+=row.commands;
            if(row.transaction && !row.missing) {
              const key=EJSON.stringify(row.transaction); observed.add(key);
              const set=owners.get(key)||new Set(); set.add(JSON.stringify([row.owner,row.observerId])); owners.set(key,set);
            }
            delete row.missing; delete row.transaction;
            for(const field of Object.keys(fields)) { row[field]=Number(g[field]); if(!Number.isFinite(row[field]) || row[field]<0) throw new Error('Invalid metric '+field); }
            rows.push(row);
          }
          systemOwnerSamples.push(...p.find({...filter,'command.q._id':'$system.patterns',op:'update'},
            {ns:1,op:1,ts:1,'command.q':1}).limit(2).toArray());
          if(samples.length<20) samples.push(...p.find(filter,{ns:1,op:1,ts:1,'command.q':1,'command.filter':1,'command.u.details':1,'command.u.request.observerKey':1,'command.lsid':1,'command.txnNumber':1,'command.autocommit':1}).limit(Math.min(8,20-samples.length)).toArray());
        }
        const admin=db.getSiblingDB('admin'), transactions=[];
        for(const r of admin.system.profile.find({$or:[{'command.commitTransaction':{$exists:true}},{'command.abortTransaction':{$exists:true}}]}, {op:1,ns:1,command:1,lsid:1,txnNumber:1,responseLength:1,docsExamined:1}).toArray()) {
          const c=r.command||{}, session=c.lsid?.id??r.lsid?.id, number=c.txnNumber??r.txnNumber;
          const key=session!==undefined && number!==undefined ? EJSON.stringify({session,number}) : null, set=owners.get(key);
          const [owner,observerId]=!set?['uncorrelated','']:set.size===1?JSON.parse([...set][0]):['shared',''];
          transactions.push({op:r.op,ns:r.ns,command:Object.keys(c)[0],owner,observerId,sessionTransaction:key,responseBytes:Number(r.responseLength||0),documentsExamined:Number(r.docsExamined||0)});
        }
        print(JSON.stringify({databases:names,boundary,rows,samples,systemOwnerSamples,transactions,profileEntries,unparsed:0,
          transactionSet:[...observed],transactionsObserved:observed.size,terminalCommandsProfiled:transactions.length,terminalCostProfiled:false,
          transactionIdentityMissing,serverStart,serverEnd,serverCommittedDelta,serverAbortedDelta,
          transactionCounterMethod:'explicit run-bounded serverStatus().transactions delta; task-owned single-tenant Mongo; terminal cost NOT PROFILED',
          commands:rows.reduce((n,r)=>n+r.commands,0)+transactions.length}));
        """);

    internal Task<BsonDocument[]> Observers() => Read($"{store}+es", "observers");
    internal Task<BsonDocument[]> State() => Read($"{store}+es+Default", "observers");
    internal Task<BsonDocument[]> Jobs() => Read($"{store}+es+Default", "jobs");
    internal Task<BsonDocument[]> JobSteps() => Read($"{store}+es+Default", "job-steps");
    internal Task<BsonDocument[]> FailedJobSteps() => Read($"{store}+es+Default", "failed-job-steps");
    internal Task<BsonDocument[]> FailedPartitions() => Read($"{store}+es+Default", "failed-partitions");

    internal async Task<ulong?> SequenceTail(string sequence)
    {
        using var cursor = await _client.GetDatabase($"{store}+es+Default").GetCollection<BsonDocument>(sequence).FindAsync(
            FilterDefinition<BsonDocument>.Empty, new FindOptions<BsonDocument> { Sort = new BsonDocument("_id", -1), Limit = 1, Comment = Comment });
        var row = (await cursor.ToListAsync()).SingleOrDefault();
        return row is null ? null : (ulong)row["_id"].ToDecimal();
    }

    async Task<BsonDocument[]> JobWrites()
    {
        var pipeline = new BsonDocument[]
        {
            new("$match", new BsonDocument { { "ns", $"{store}+es+Default.jobs" }, { "op", new BsonDocument("$in", new BsonArray { "update", "remove" }) } }),
            new("$sort", new BsonDocument("ts", -1)),
            new("$group", new BsonDocument { { "_id", new BsonDocument { { "id", "$command.q._id" }, { "op", "$op" } } }, { "row", new BsonDocument("$first", "$$ROOT") } }),
            new("$replaceRoot", new BsonDocument("newRoot", "$row")),
            new("$project", new BsonDocument { { "ns", 1 }, { "op", 1 }, { "command", 1 }, { "ts", 1 }, { "ndeleted", 1 } })
        };
        using var cursor = await _client.GetDatabase($"{store}+es+Default").GetCollection<BsonDocument>("system.profile")
            .AggregateAsync<BsonDocument>(pipeline, new AggregateOptions { Comment = Comment, AllowDiskUse = true });
        return (await cursor.ToListAsync()).ToArray();
    }

    internal async Task<BsonDocument[]> CompletedCatchupJobs(string observerId, ulong tail)
    {
        _jobWrites ??= JobWrites(); // One compact tail-time read, never a repeated whole-profiler poll.
        var writes = await _jobWrites;
        JobWriteSamples = writes.Select(d => d.ToJson()).ToArray();
        var matching = writes.Where(r => r["op"] == "update" && r["command"]["u"].AsBsonDocument.GetValue("details", "") == observerId);
        var completed = new List<BsonDocument>();
        foreach (var write in matching)
        {
            var job = write["command"]["u"].AsBsonDocument;
            var progress = job["progress"].AsBsonDocument;
            var key = job["request"]["observerKey"].AsBsonDocument;
            var removal = writes.SingleOrDefault(r => r["op"] == "remove" && r["command"]["q"]["_id"].Equals(job["_id"]) &&
                r.GetValue("ndeleted", 0).ToInt32() == 1 && r["ts"].AsBsonDateTime.MillisecondsSinceEpoch >= write["ts"].AsBsonDateTime.MillisecondsSinceEpoch);
            if (job["type"] != "CatchUpObserver" || key["observerId"] != observerId || key["eventStore"] != store || key["namespace"] != "Default" || key["eventSequenceId"] != "event-log") continue;
            var last = job["lastHandledEventSequenceNumber"].ToDecimal();
            if (progress["totalSteps"].ToInt32() <= 0 || progress["successfulSteps"] != progress["totalSteps"] ||
                progress["failedSteps"].ToInt32() != 0 || progress["stoppedSteps"].ToInt32() != 0 || progress["unreachableSteps"].ToInt32() != 0 ||
                !job["handledAllEvents"].AsBoolean || last < tail || last >= (decimal)(ulong.MaxValue - 2) || removal is null) continue;
            completed.Add(new BsonDocument { { "job", job }, { "writeTimestamp", write["ts"] }, { "removalTimestamp", removal["ts"] } });
        }
        if ((await Jobs()).Any(j => j.GetValue("details", "") == observerId)) throw new InvalidOperationException("A second non-deleted probe job remains at tail.");
        return completed.ToArray();
    }

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
