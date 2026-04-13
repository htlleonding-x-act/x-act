import 'dart:convert';
import 'dart:math';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:latlong2/latlong.dart';

import '../services/app_session.dart';
import 'api_config.dart';
import 'models.dart';

part 'api_service_types.dart';
part 'api_service_data.dart';
part 'api_service_session.dart';
part 'api_service_http.dart';

final class ApiService {
  ApiService._({required String baseUrl})
    : _baseUri = Uri.parse(baseUrl),
      _http = http.Client();

  static final ApiService instance = ApiService._(baseUrl: ApiConfig.baseUrl);

  final Uri _baseUri;
  final http.Client _http;
  final AppSession _session = AppSession.instance;

 
}