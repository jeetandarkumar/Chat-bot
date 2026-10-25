// The user wants me to apply the suggested edit to the original code and output the complete modified file. I need to carefully merge the changes while preserving all unchanged code.

// Let me analyze the differences:

// 1. Added import: `import 'package:logger/logger.dart';`
// 2. Added `final Logger _logger = Logger();` field in DioClient
// 3. Changed `_LoggingInterceptor()` to `_LoggingInterceptor(_logger)` in the interceptors list
// 4. Modified `_LoggingInterceptor` class to accept Logger in constructor and use it instead of print
// 5. Modified `_ConnectivityInterceptor` to use `connectivityResult.contains(ConnectivityResult.none)` instead of `connectivityResult == ConnectivityResult.none`

// I'll output the complete modified file with these changes applied```
import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:logger/logger.dart';
import '../config/app_config.dart';

class DioClient {
  static final DioClient _instance = DioClient._internal();
  factory DioClient() => _instance;
  DioClient._internal();

  late final Dio _dio;
  final FlutterSecureStorage _storage = const FlutterSecureStorage();
  final Connectivity _connectivity = Connectivity();
  final Logger _logger = Logger();

  Dio get dio => _dio;

  void init() {
    _dio = Dio(BaseOptions(
      baseUrl: AppConfig.baseUrl,
      connectTimeout: Duration(milliseconds: AppConfig.connectTimeout),
      receiveTimeout: Duration(milliseconds: AppConfig.receiveTimeout),
      sendTimeout: Duration(milliseconds: AppConfig.sendTimeout),
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
      },
    ));

    _dio.interceptors.addAll([
      _AuthInterceptor(_storage),
      _LoggingInterceptor(_logger),
      _ConnectivityInterceptor(_connectivity),
      _ErrorInterceptor(),
    ]);
  }

  Future<void> setAuthToken(String token) async {
    await _storage.write(key: 'access_token', value: token);
  }

  Future<String?> getAuthToken() async {
    return await _storage.read(key: 'access_token');
  }

  Future<void> setRefreshToken(String token) async {
    await _storage.write(key: 'refresh_token', value: token);
  }

  Future<String?> getRefreshToken() async {
    return await _storage.read(key: 'refresh_token');
  }

  Future<void> clearTokens() async {
    await _storage.delete(key: 'access_token');
    await _storage.delete(key: 'refresh_token');
  }

  Future<bool> hasValidToken() async {
    final token = await getAuthToken();
    return token != null && token.isNotEmpty;
  }
}

class _AuthInterceptor extends Interceptor {
  final FlutterSecureStorage _storage;

  _AuthInterceptor(this._storage);

  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) async {
    final publicPaths = [
      '/api/auth/login',
      '/api/auth/register',
      '/api/auth/verify-otp',
      '/api/auth/refresh',
    ];

    if (publicPaths.any((path) => options.path.contains(path))) {
      return handler.next(options);
    }

    final token = await _storage.read(key: 'access_token');
    if (token != null && token.isNotEmpty) {
      options.headers['Authorization'] = 'Bearer $token';
    }
    handler.next(options);
  }
}

class _LoggingInterceptor extends Interceptor {
  final Logger _logger;

  _LoggingInterceptor(this._logger);

  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) {
    _logger.d('[DIO REQUEST] ${options.method} ${options.uri}');
    if (options.data != null) {
      _logger.d('[DIO REQUEST DATA] ${options.data}');
    }
    handler.next(options);
  }

  @override
  void onResponse(Response response, ResponseInterceptorHandler handler) {
    _logger.d('[DIO RESPONSE] ${response.statusCode} ${response.requestOptions.uri}');
    handler.next(response);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) {
    _logger.e('[DIO ERROR] ${err.response?.statusCode} ${err.requestOptions.uri}: ${err.message}');
    handler.next(err);
  }
}

class _ConnectivityInterceptor extends Interceptor {
  final Connectivity _connectivity;

  _ConnectivityInterceptor(this._connectivity);

  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) async {
    final connectivityResult = await _connectivity.checkConnectivity();
    if (connectivityResult.contains(ConnectivityResult.none)) {
      return handler.reject(DioException(
        requestOptions: options,
        error: 'No internet connection',
        type: DioExceptionType.connectionError,
      ));
    }
    handler.next(options);
  }
}

class _ErrorInterceptor extends Interceptor {
  @override
  void onError(DioException err, ErrorInterceptorHandler handler) {
    if (err.response?.statusCode == 401) {
      // Token expired - could trigger refresh
    }
    handler.next(err);
  }
}