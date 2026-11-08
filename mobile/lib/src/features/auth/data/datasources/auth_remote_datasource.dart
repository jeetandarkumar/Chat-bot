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
      return Right(_parseAuthResult(response.data as Map<String, dynamic>));
    } on DioException catch (e) {
      return Left(_handleError(e));
    } catch (_) {
      return Left(_unexpectedResponse());
    }
  }

  @override
  Future<Either<AppException, AuthResult>> register(String phoneNumber, String? displayName) async {
    try {
      final response = await _dioClient.dio.post('/api/auth/register', data: {
        'phoneNumber': phoneNumber,
        'displayName': displayName,
      });
      return Right(_parseAuthResult(response.data as Map<String, dynamic>));
    } on DioException catch (e) {
      return Left(_handleError(e));
    } catch (_) {
      return Left(_unexpectedResponse());
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

      // Parse everything first so a malformed response never leaves half-saved tokens behind.
      final data = response.data as Map<String, dynamic>;
      final accessToken = data['accessToken'] as String;
      final refreshToken = data['refreshToken'] as String;
      final userModel = UserModel.fromJson(data['user'] as Map<String, dynamic>);

      await _dioClient.setAuthToken(accessToken);
      await _dioClient.setRefreshToken(refreshToken);

      return Right(AuthTokens(
        accessToken: accessToken,
        refreshToken: refreshToken,
        user: userModel.toEntity(),
      ));
    } on DioException catch (e) {
      return Left(_handleError(e));
    } catch (_) {
      return Left(_unexpectedResponse());
    }
  }

  @override
  Future<Either<AppException, UserModel>> getCurrentUser() async {
    try {
      final response = await _dioClient.dio.get('/api/auth/me');
      return Right(UserModel.fromJson(response.data as Map<String, dynamic>));
    } on DioException catch (e) {
      return Left(_handleError(e));
    } catch (_) {
      return Left(_unexpectedResponse());
    }
  }

  @override
  Future<Either<AppException, void>> logout() async {
    try {
      final refreshToken = await _dioClient.getRefreshToken();
      await _dioClient.dio.post('/api/auth/logout', data: {
        'refreshToken': refreshToken,
      });
    } catch (_) {
      // Logging out locally must always work, even if the server is unreachable
      // or the session has already expired.
    }
    await _dioClient.clearTokens();
    return const Right(null);
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

      final data = response.data as Map<String, dynamic>;
      final newAccessToken = data['accessToken'] as String;
      final newRefreshToken = data['refreshToken'] as String;
      final userModel = UserModel.fromJson(data['user'] as Map<String, dynamic>);

      await _dioClient.setAuthToken(newAccessToken);
      await _dioClient.setRefreshToken(newRefreshToken);

      return Right(AuthTokens(
        accessToken: newAccessToken,
        refreshToken: newRefreshToken,
        user: userModel.toEntity(),
      ));
    } on DioException catch (e) {
      return Left(_handleError(e));
    } catch (_) {
      return Left(_unexpectedResponse());
    }
  }

  AuthResult _parseAuthResult(Map<String, dynamic> data) {
    final requiresOtp = data['requiresOtp'] as bool? ?? false;
    if (requiresOtp) {
      return AuthResult.requiresOtp(
        purpose: OtpPurpose.values.firstWhere(
          (e) => e.name.toLowerCase() == (data['otpPurpose'] as String?)?.toLowerCase(),
          orElse: () => OtpPurpose.login,
        ),
        message: data['message'] as String?,
      );
    }
    return AuthResult.success(data['message'] as String? ?? 'Success');
  }

  AppException _unexpectedResponse() {
    return const UnknownException('Unexpected response from server. Please try again.');
  }

  /// The error body is not always JSON with a "message": a 401 from the JWT middleware has
  /// an empty body and a development 500 is an HTML page. Never assume its shape.
  String _extractMessage(DioException e, String fallback) {
    final data = e.response?.data;
    if (data is Map) {
      final message = data['message'] ?? data['title'];
      if (message is String && message.isNotEmpty) {
        return message;
      }
    }
    return fallback;
  }

  AppException _handleError(DioException e) {
    switch (e.type) {
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.sendTimeout:
      case DioExceptionType.receiveTimeout:
        return const NetworkException('Connection timeout. Please try again.');
      case DioExceptionType.connectionError:
        return const NetworkException('Cannot reach the server. Check your connection.');
      case DioExceptionType.badResponse:
        final statusCode = e.response?.statusCode;
        switch (statusCode) {
          case 400:
            return ValidationException(_extractMessage(e, 'Invalid request'));
          case 401:
            return AuthenticationException(_extractMessage(e, 'Session expired. Please log in again.'));
          case 403:
            return AuthorizationException(_extractMessage(e, 'You are not allowed to do that'));
          case 404:
            return NotFoundException(_extractMessage(e, 'Not found'));
          case 409:
            return ConflictException(_extractMessage(e, 'Conflict'));
          default:
            if (statusCode != null && statusCode >= 500) {
              return ServerException(_extractMessage(e, 'Server error. Please try again later.'));
            }
            return UnknownException(
              _extractMessage(e, 'Unexpected error'),
              code: statusCode?.toString(),
            );
        }
      default:
        return UnknownException(e.message ?? 'Unknown error');
    }
  }
}
