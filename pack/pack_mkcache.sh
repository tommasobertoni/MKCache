#!/bin/bash

remote_pack_file_url="https://gist.githubusercontent.com/tommasobertoni/b6908c192edafe1e3a50151e0ad72ea6/raw/8bb95b7583bf10dc555e1f4df0feb301877e502f/pack.sh"
pack_file="pack.sh"

if ! test -f "$pack_file"; then
    echo "Downloading pack.sh..."
    wget -O $pack_file $remote_pack_file_url -q
fi

chmod a+x $pack_file
bash $pack_file -P "../src/MKCache/MKCache.csproj" -B -S -O "tommasobertoni" -L "../LICENSE"
