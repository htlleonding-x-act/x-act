import 'dart:io';

Future<HttpServer?> startLocalCallbackServer(
  int port,
  void Function(String code) onCode,
) async {
  try {
    final server = await HttpServer.bind(InternetAddress.loopbackIPv4, port);
    server.listen((request) async {
      final code = request.uri.queryParameters['code'];
      request.response
        ..statusCode = 200
        ..headers.contentType = ContentType.html
        ..write(
          '<html><body style="font-family:sans-serif;text-align:center;margin-top:4em">'
          '<h2>Login erfolgreich ✓</h2>'
          '<p>Du kannst diesen Tab jetzt schließen.</p>'
          '</body></html>',
        );
      await request.response.close();
      if (code != null) onCode(code);
    });
    return server;
  } catch (e) {
    return null;
  }
}

Future<void> stopLocalCallbackServer(Object? server) async {
  if (server is HttpServer) await server.close(force: true);
}
