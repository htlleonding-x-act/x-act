import 'dart:convert';
import 'dart:math';

import 'package:crypto/crypto.dart';

/// PKCE verifier and CSRF `state` for a single login attempt (RFC 7636).
///
/// The app is a public OAuth client, so without PKCE an intercepted authorization
/// code could be redeemed by anyone — on Android any installed app may claim the
/// unverified `xact://` scheme. Only the holder of the verifier can exchange the
/// code, and `state` ties the callback back to the attempt that started it.
class AuthChallenge {
  const AuthChallenge({required this.verifier, required this.state});

  AuthChallenge.generate()
    : verifier = _randomValue(64),
      state = _randomValue(32);

  final String verifier;
  final String state;

  String get challenge =>
      _base64Url(sha256.convert(ascii.encode(verifier)).bytes);

  static String _randomValue(int byteCount) {
    final random = Random.secure();
    return _base64Url(
      List<int>.generate(byteCount, (_) => random.nextInt(256)),
    );
  }

  // unpadded base64url is the only encoding rfc 7636 accepts
  static String _base64Url(List<int> bytes) =>
      base64UrlEncode(bytes).replaceAll('=', '');
}
