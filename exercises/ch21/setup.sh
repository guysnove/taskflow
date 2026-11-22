#!/usr/bin/env bash
# Chapter 21: a small "team" on one computer, so you can practise collaboration without anyone else.
#   team/origin.git  a bare repository: it plays the part of GitHub (the shared remote)
#   team/ana, team/ben  two developers' clones, each with their own name and email
# Usage: ./setup.sh            (creates ./team; deletes an old one first)
set -euo pipefail
here="$(cd "$(dirname "$0")" && pwd)"; team="${TEAM:-$here/team}"
rm -rf "$team" && mkdir -p "$team"

git init -q --bare -b main "$team/origin.git"

# The first commit: the project as it is at the start of the chapter.
git clone -q "$team/origin.git" "$team/ana" 2>/dev/null
cp -r "$here/start/." "$team/ana/"
cd "$team/ana"
git config user.name "Ana Lopez"; git config user.email "ana@example.com"
git add -A && git commit -q -m "Start the invoice library: lines, VAT and a total"
git push -q origin main

git clone -q "$team/origin.git" "$team/ben"
cd "$team/ben" && git config user.name "Ben Okafor" && git config user.email "ben@example.com"

echo "Team ready in $team: origin.git (the shared remote), ana/ and ben/ (two developers)."
