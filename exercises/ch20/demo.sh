#!/usr/bin/env bash
# Chapter 20: run the notes service, then send it real HTTP requests with curl.
#   ./demo.sh sqli | xss | idor | redirect
set -e
cd "$(dirname "$0")"
rm -f /tmp/ch20-demo.db
ConnectionStrings__Notes="Data Source=/tmp/ch20-demo.db" dotnet run --project Ch20.Notes --urls http://127.0.0.1:5020 > /tmp/ch20-demo.log 2>&1 &
server=$!
trap 'kill $server 2>/dev/null' EXIT
for _ in $(seq 60); do curl -s -o /dev/null http://127.0.0.1:5020/good/hello?name=x && break; sleep 1; done
get() { echo "\$ curl -i -H 'X-User: $1' 'http://127.0.0.1:5020$2'"; curl -s -i -H "X-User: $1" "http://127.0.0.1:5020$2" | tr -d '\r' | grep -vE '^(Date|Server|Transfer-Encoding):'; echo; echo; }
case "$1" in
  sqli)
    get ana "/bad/search?q=milk"
    get ana "/bad/search?q='%20OR%201=1%20--"
    get ana "/good/search?q='%20OR%201=1%20--" ;;
  xss)
    get ana "/bad/hello?name=%3Cscript%3Ealert('stolen')%3C/script%3E"
    get ana "/good/hello?name=%3Cscript%3Ealert('stolen')%3C/script%3E" ;;
  idor)
    get ben "/bad/notes/1"
    get ben "/good/notes/1"
    get ana "/good/notes/1" ;;
  redirect)
    get ana "/bad/go?to=https://evil.example/login"
    get ana "/good/go?to=https://evil.example/login" ;;
  *) echo "usage: ./demo.sh sqli | xss | idor | redirect"; exit 1 ;;
esac
