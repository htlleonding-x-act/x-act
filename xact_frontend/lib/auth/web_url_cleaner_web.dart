import 'dart:js_interop';

@JS('history.replaceState')
external void _replaceState(JSAny? data, String title, String url);

@JS('window.location.replace')
external void _locationReplace(String url);

void cleanBrowserUrl() => _replaceState(null, '', '/');
void navigateBrowserTo(String url) => _locationReplace(url);
