import 'dart:js_interop';

@JS('history.replaceState')
external void _replaceState(JSAny? data, String title, String url);

void cleanBrowserUrl() => _replaceState(null, '', '/');
