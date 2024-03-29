#!/bin/bash
cd $1
target_file=$2
target=$(find . -maxdepth 1 -type f -name "*.csproj" -print -quit)
project=$(basename "$target")
project="${project%.*}"
assembly_name=$(sed -n 's/.*<AssemblyName>\(.*\)<\/AssemblyName>.*/\1/p' "$target")
target_path="$PWD/bin/Debug/net8.0"
if [ -n "$assembly_name" ]; then
    target_path="$target_path/$assembly_name.dll"
else
    target_path="$target_path/$project.dll"
fi
set target_path="$target_path"
echo "$target_path"
echo "$target_path" > "$target_file"

