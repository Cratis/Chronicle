#!/bin/bash
kill -9 $(lsof -t -i :5500 -s TCP:LISTEN)
#sudo
dotnet run -c Release -- @run.rsp
