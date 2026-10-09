#!/bin/sh
printf '\033c\033]0;%s\a' Cellular Automata
base_path="$(dirname "$(realpath "$0")")"
"$base_path/Cellular Automata.x86_64" "$@"
