#!/bin/bash
kill -9 $(lsof -t -i :5500 -s TCP:LISTEN)
#sudo
echo $PWD
dotnet run -c Release -- @./run.rsp
cp ./results/results/BenchmarkRun-joined*.json ./results/results.json
