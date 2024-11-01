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
rm -rf ./out
