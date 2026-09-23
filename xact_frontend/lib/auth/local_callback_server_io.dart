import 'dart:io';

Future<HttpServer?> startLocalCallbackServer(
  int port,
  void Function(Uri uri) onCallback,
) async {
  try {
    final server = await HttpServer.bind(InternetAddress.loopbackIPv4, port);
    server.listen((request) async {
      request.response
        ..statusCode = 200
        ..headers.contentType = ContentType.html
        ..write(
          '<html><body style="font-family:sans-serif;text-align:center;margin-top:4em">'
          '<h2>Signed in ✓</h2>'
          '<p>You can close this tab now.</p>'
          '</body></html>',
        );
      await request.response.close();
      onCallback(request.uri);
    });
    return server;
  } catch (_) {
    return null;
  }
}

Future<void> stopLocalCallbackServer(Object? server) async {
  if (server is HttpServer) await server.close(force: true);
}
