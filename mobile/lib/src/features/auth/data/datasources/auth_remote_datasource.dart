import 'package:dartz/dartz.dart';
import 'package:chat_app/src/features/auth/domain/repositories/auth_repository.dart';
import 'package:chat_app/src/features/auth/data/models/user_model.dart';
import 'package:chat_app/src/core/errors/exceptions.dart';
import 'package:chat_app/src/core/network/dio_client.dart';
import 'package:dio/dio.dart' show DioException, DioExceptionType;

abstract class AuthRemoteDataSource {
  Future<Either<AppException, AuthResult>> login(String phoneNumber);
  Future<Either<AppException, AuthResult>> register(String phoneNumber, String? displayName);
  Future<Either<AppException, AuthTokens>> verifyOtp(String phoneNumber, String code, OtpPurpose purpose);
  Future<Either<AppException, UserModel>> getCurrentUser();
  Future<Either<AppException, void>> logout();
  Future<Either<AppException, AuthTokens>> refreshToken();
}

class AuthRemoteDataSourceImpl implements AuthRemoteDataSource {
  final DioClient _dioClient;

  AuthRemoteDataSourceImpl(this._dioClient);

  @override
  Future<Either<AppException, AuthResult>> login(String phoneNumber) async {
    try {
      final response = await _dioClient.dio.post('/api/auth/login', data: {
        'phoneNumber': phoneNumber,
      });
      return Right(_parseAuthResult(response.data));
    } on DioException catch (e) {
      return Left(_handleError(e));
    }
  }

  @override
  Future<Either<AppException, AuthResult>> register(String phoneNumber, String? displayName) async {
    try {
      final response = await _dioClient.dio.post('/api/auth/register', data: {
        'phoneNumber': phoneNumber,
        'displayName': displayName,
      });
      return Right(_parseAuthResult(response.data));
    } on DioException catch (e) {
      return Left(_handleError(e));
    }
  }

  @override
  Future<Either<AppException, AuthTokens>> verifyOtp(String phoneNumber, String code, OtpPurpose purpose) async {
    try {
      final response = await _dioClient.dio.post('/api/auth/verify-otp', data: {
        'phoneNumber': phoneNumber,
        'code': code,
        'purpose': purpose.name,
      });

      final data = response.data;
      final accessToken = data['accessToken'] as String;
      final refreshToken = data['refreshToken'] as String;

      await _dioClient.setAuthToken(accessToken);
      await _dioClient.setRefreshToken(refreshToken);

      final userModel = UserModel.fromJson(data['user'] as Map<String, dynamic>);
      return Right(AuthTokens(
        accessToken: accessToken,
        refreshToken: refreshToken,
        user: userModel.toEntity(),
      ));
    } on DioException catch (e) {
      return Left(_handleError(e));
    }
  }

  @override
  Future<Either<AppException, UserModel>> getCurrentUser() async {
    try {
      final response = await _dioClient.dio.get('/api/auth/me');
      return Right(UserModel.fromJson(response.data as Map<String, dynamic>));
    } on DioException catch (e) {
      return Left(_handleError(e));
    }
  }

  @override
  Future<Either<AppException, void>> logout() async {
    try {
      await _dioClient.dio.post('/api/auth/logout');
      await _dioClient.clearTokens();
      return const Right(null);
    } on DioException catch (e) {
      return Left(_handleError(e));
    }
  }

  @override
  Future<Either<AppException, AuthTokens>> refreshToken() async {
    try {
      final refreshToken = await _dioClient.getRefreshToken();
      if (refreshToken == null) {
        return Left(const AuthenticationException('No refresh token available'));
      }

      final response = await _dioClient.dio.post('/api/auth/refresh', data: {
        'refreshToken': refreshToken,
      });

      final data = response.data;
      final newAccessToken = data['accessToken'] as String;
      final newRefreshToken = data['refreshToken'] as String;

      await _dioClient.setAuthToken(newAccessToken);
      await _dioClient.setRefreshToken(newRefreshToken);

      final userModel = UserModel.fromJson(data['user'] as Map<String, dynamic>);
      return Right(AuthTokens(
        accessToken: newAccessToken,
        refreshToken: newRefreshToken,
        user: userModel.toEntity(),
      ));
    } on DioException catch (e) {
      return Left(_handleError(e));
    }
  }

  AuthResult _parseAuthResult(Map<String, dynamic> data) {
    final requiresOtp = data['requiresOtp'] as bool? ?? false;
    if (requiresOtp) {
      return AuthResult.requiresOtp(
        purpose: OtpPurpose.values.firstWhere(
          (e) => e.name == data['otpPurpose'],
          orElse: () => OtpPurpose.login,
        ),
        message: data['message'] as String?,
      );
    }
    return AuthResult.success(data['message'] as String? ?? 'Success');
  }

  AppException _handleError(DioException e) {
    switch (e.type) {
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.sendTimeout:
      case DioExceptionType.receiveTimeout:
        return const NetworkException('Connection timeout');
      case DioExceptionType.connectionError:
        return const NetworkException('No internet connection');
      case DioExceptionType.badResponse:
        final statusCode = e.response?.statusCode;
        final message = e.response?.data?['message'] as String? ?? 'Unknown error';
        switch (statusCode) {
          case 400:
            return ValidationException(message);
          case 401:
            return AuthenticationException(message);
          case 403:
            return AuthorizationException(message);
          case 404:
            return NotFoundException(message);
          case 409:
            return ConflictException(message);
          case 500:
            return ServerException(message);
          default:
            return UnknownException(message, code: statusCode.toString());
        }
      default:
        return UnknownException(e.message ?? 'Unknown error');
    }
  }
}