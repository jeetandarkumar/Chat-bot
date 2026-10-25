import 'package:dartz/dartz.dart';
import 'package:chat_app/src/features/auth/domain/repositories/auth_repository.dart';
import 'package:chat_app/src/core/errors/exceptions.dart';

class RegisterUseCase {
  final AuthRepository _repository;

  RegisterUseCase(this._repository);

  Future<Either<AppException, AuthResult>> call(String phoneNumber, String? displayName) async {
    // Validate phone number format
    if (phoneNumber.trim().isEmpty) {
      return Left(const ValidationException('Phone number is required'));
    }

    final cleaned = phoneNumber.replaceAll(RegExp(r'[\s\-()]'), '');
    if (!cleaned.startsWith('+') || cleaned.length < 10) {
      return Left(const ValidationException('Invalid phone number format. Use international format (e.g., +1234567890)'));
    }

    // Validate display name if provided
    if (displayName != null && displayName.trim().isEmpty) {
      return Left(const ValidationException('Display name cannot be empty if provided'));
    }

    if (displayName != null && displayName.trim().length > 100) {
      return Left(const ValidationException('Display name must be less than 100 characters'));
    }

    return await _repository.register(cleaned, displayName?.trim());
  }
}