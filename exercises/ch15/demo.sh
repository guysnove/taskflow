#!/usr/bin/env bash
# Chapter 15 demo: starts the Library API, sends real HTTP requests with curl, shows the answers.
# Usage: ./demo.sh bad | rest | errors | versions | paging | idempotency | docs
set -e
cd "$(dirname "$0")"
URL=http://localhost:5015
dotnet build Ch15.Api -nologo -v q > /dev/null
dotnet run --project Ch15.Api --no-build --urls $URL > /tmp/ch15-api.log 2>&1 &
SERVER=$!
trap 'kill $SERVER 2>/dev/null' EXIT
until curl -s -o /dev/null $URL/v1/books; do sleep 0.3; done

# req METHOD PATH [JSON body] [extra header]: prints the request, the status, the useful headers and the body.
req() {
  local method=$1 path=$2 body=${3:-} header=${4:-}
  echo "▶ $method $path ${header:+[$header] }${body}"
  local args=(-s -D - -X "$method" "$URL$path")
  [ -n "$body" ] && args+=(-H "Content-Type: application/json" -d "$body")
  [ -n "$header" ] && args+=(-H "$header")
  curl "${args[@]}" | tr -d '\r' | grep -iE '^(HTTP/|content-type:|location:|idempotent-replayed:)|^[[{]' | sed 's/^/  /'
  echo
}

case "${1:-rest}" in
  bad)
    req GET "/bad/getBook?id=3"
    req GET "/bad/getBook?id=99"
    req POST "/bad/deleteBook?id=99" ;;
  rest)
    req GET /v1/books/3
    req POST /v1/books '{"title":"Team Topologies","authors":["Matthew Skelton","Manuel Pais"],"year":2019,"copies":2}'
    req GET /v1/books/9
    req DELETE /v1/books/9
    req DELETE /v1/books/9 ;;
  errors)
    req GET /v1/books/99
    req POST /v1/books '{"title":"","authors":[],"year":3000}'
    req GET /v1/nothing-here
    req POST /v1/books 'title=Team Topologies' ;;
  versions)
    req GET /v1/books/5
    req GET /v2/books/5 ;;
  paging)
    req GET "/v2/books?pageSize=3"
    req GET "/v2/books?page=3&pageSize=3"
    req GET "/v2/books?author=martin&available=true"
    req GET "/v2/books?pageSize=500" ;;
  idempotency)
    req POST /v1/loans '{"bookId":4,"member":"ana"}' "Idempotency-Key: 7f1c2b"
    req POST /v1/loans '{"bookId":4,"member":"ana"}' "Idempotency-Key: 7f1c2b"
    req POST /v1/loans '{"bookId":4,"member":"ben"}' "Idempotency-Key: 7f1c2b"
    req GET /v1/loans/count
    req POST /v1/loans '{"bookId":4,"member":"ana"}'
    req POST /v1/loans '{"bookId":4,"member":"ana"}'
    req GET /v1/loans/count ;;
  docs)
    echo "▶ GET /openapi/v1.json   (the paths, and what GET /v1/books/{id} can answer)"
    curl -s $URL/openapi/v1.json | python3 -c "
import json, sys
d = json.load(sys.stdin)
print('  openapi', d['openapi'], '·', d['info']['title'])
for path, item in d['paths'].items():
    print('  ', ' '.join(m.upper() for m in item), path)
r = d['paths']['/v1/books/{id}']['get']['responses']
print('  GET /v1/books/{id} answers:', ', '.join(f'{c} {v[\"description\"]}' for c, v in r.items()))
" ;;
esac
