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

rm -rf ./out
