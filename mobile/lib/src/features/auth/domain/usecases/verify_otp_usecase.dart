import 'package:dartz/dartz.dart';
import 'package:chat_app/src/features/auth/domain/repositories/auth_repository.dart';
import 'package:chat_app/src/core/errors/exceptions.dart';

class VerifyOtpUseCase {
  final AuthRepository _repository;

  VerifyOtpUseCase(this._repository);

  Future<Either<AppException, AuthTokens>> call(String phoneNumber, String code, OtpPurpose purpose) async {
    // Validate inputs
    if (phoneNumber.trim().isEmpty) {
      return Left(const ValidationException('Phone number is required'));
    }

    final cleanedPhone = phoneNumber.replaceAll(RegExp(r'[\s\-()]'), '');
    if (!cleanedPhone.startsWith('+') || cleanedPhone.length < 10) {
      return Left(const ValidationException('Invalid phone number format. Use international format (e.g., +1234567890)'));
    }

    if (code.trim().isEmpty) {
      return Left(const ValidationException('OTP code is required'));
    }

    if (code.length != 6) {
      return Left(const ValidationException('OTP code must be 6 digits'));
    }

    if (!RegExp(r'^\d{6}$').hasMatch(code)) {
      return Left(const ValidationException('OTP code must contain only digits'));
    }

    return await _repository.verifyOtp(cleanedPhone, code, purpose);
  }
}