import 'package:dartz/dartz.dart';
import 'package:chat_app/src/features/auth/domain/entities/user.dart';
import 'package:chat_app/src/core/errors/exceptions.dart';

abstract class AuthRepository {
  Future<Either<AppException, AuthResult>> login(String phoneNumber);
  Future<Either<AppException, AuthResult>> register(String phoneNumber, String? displayName);
  Future<Either<AppException, AuthTokens>> verifyOtp(String phoneNumber, String code, OtpPurpose purpose);
  Future<Either<AppException, User>> getCurrentUser();
  Future<Either<AppException, void>> logout();
  Future<Either<AppException, AuthTokens>> refreshToken();
}

enum OtpPurpose { registration, login, phoneNumberChange }

class AuthResult {
  final bool requiresOtp;
  final String? message;
  final OtpPurpose? otpPurpose;

  const AuthResult({
    required this.requiresOtp,
    this.message,
    this.otpPurpose,
  });

  factory AuthResult.requiresOtp({required OtpPurpose purpose, String? message}) {
    return AuthResult(
      requiresOtp: true,
      otpPurpose: purpose,
      message: message,
    );
  }

  factory AuthResult.success(String message) {
    return AuthResult(
      requiresOtp: false,
      message: message,
    );
  }
}

class AuthTokens {
  final String accessToken;
  final String refreshToken;
  final User user;

  const AuthTokens({
    required this.accessToken,
    required this.refreshToken,
    required this.user,
  });
}