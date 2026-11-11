#!/bin/bash
CPU=$(dpkg --print-architecture)
if [ $CPU == "amd64" ]; then
    ARCH_FOLDER="x64"
else
    ARCH_FOLDER="arm64"
fi

cp ./out/$ARCH_FOLDER/*.dll .
cp ./out/$ARCH_FOLDER/*.json .
cp ./out/$ARCH_FOLDER/*.xml .
cp ./out/$ARCH_FOLDER/*.so .
cp ./out/$ARCH_FOLDER/Cratis.Chronicle.Server .

# Restore the executable bit on the apphost. The published binary loses it in transit
# (actions/upload-artifact does not preserve Unix permissions), and every entrypoint
# invokes ./Cratis.Chronicle.Server directly — without +x the container exits 126.
chmod +x Cratis.Chronicle.Server

# A self-contained publish carries createdump, which the runtime runs from the directory holding
# libcoreclr.so to write a crash dump (DOTNET_DbgEnableMiniDump). Without it no dump is written.
# It loses its executable bit in transit like the apphost. Framework-dependent publishes (the
# development images) do not carry it; their shared runtime has its own.
if [ -f ./out/$ARCH_FOLDER/createdump ]; then
    cp ./out/$ARCH_FOLDER/createdump .
    chmod +x createdump
fi

rm -rf ./out
