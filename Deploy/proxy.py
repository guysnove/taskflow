# A tiny reverse proxy for Chapter 29's experiments: it stands in for nginx or a cloud load balancer.
# Listens on 127.0.0.1:8080 and passes every request to TaskFlow on 127.0.0.1:5000.
# Like a real proxy that handles HTTPS for the app ("TLS termination"), it tells the app:
#   X-Forwarded-For:   the real client's address
#   X-Forwarded-Proto: https  (the client is assumed to have used HTTPS to reach the proxy)
# For learning only — use a real proxy (nginx, Caddy, YARP, your host's load balancer) in production.
import http.client, http.server, socketserver, sys

BACKEND = ("127.0.0.1", 5000)

class Proxy(http.server.BaseHTTPRequestHandler):
    protocol_version = "HTTP/1.1"

    def forward(self):
        length = int(self.headers.get("Content-Length", 0))
        body = self.rfile.read(length) if length else None
        headers = {k: v for k, v in self.headers.items() if k.lower() not in ("connection", "keep-alive")}
        headers["X-Forwarded-For"] = self.client_address[0]
        headers["X-Forwarded-Proto"] = "https"
        headers["X-Forwarded-Host"] = self.headers.get("Host", "")
        conn = http.client.HTTPConnection(*BACKEND, timeout=30)
        conn.request(self.command, self.path, body=body, headers=headers)
        resp = conn.getresponse()
        data = resp.read()
        self.send_response(resp.status, resp.reason)
        for k, v in resp.getheaders():
            if k.lower() not in ("transfer-encoding", "connection", "content-length", "date", "server"):
                self.send_header(k, v)
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        self.wfile.write(data)
        conn.close()

    do_GET = do_POST = do_PUT = do_DELETE = do_HEAD = forward

    def log_message(self, fmt, *args):
        sys.stdout.write("proxy: %s %s\n" % (self.client_address[0], fmt % args)); sys.stdout.flush()

class Server(socketserver.ThreadingMixIn, http.server.HTTPServer):
    daemon_threads = True

if __name__ == "__main__":
    print("proxy: listening on http://127.0.0.1:8080 → http://127.0.0.1:5000", flush=True)
    Server(("127.0.0.1", 8080), Proxy).serve_forever()
