#!/bin/bash
CPU=$(dpkg --print-architecture)
if [ "$CPU" == "amd64" ]; then
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

# A self-contained publish (it carries libcoreclr.so) must also carry createdump, which the runtime
# runs from the directory holding libcoreclr.so to write a crash dump (DOTNET_DbgEnableMiniDump).
# Without it no dump is written, so a self-contained output without createdump is an error rather
# than something to skip. It loses its executable bit in transit like the apphost.
# Framework-dependent publishes (the development images) carry neither; their shared runtime has its own.
if [ -f "./out/$ARCH_FOLDER/libcoreclr.so" ]; then
    if [ ! -f "./out/$ARCH_FOLDER/createdump" ]; then
        echo "error: self-contained output in ./out/$ARCH_FOLDER has libcoreclr.so but no createdump; crash dumps would not work" >&2
        exit 1
    fi
    if ! cp "./out/$ARCH_FOLDER/createdump" . || ! chmod +x createdump; then
        echo "error: failed to copy createdump from ./out/$ARCH_FOLDER or make it executable" >&2
        exit 1
    fi
fi

rm -rf ./out
