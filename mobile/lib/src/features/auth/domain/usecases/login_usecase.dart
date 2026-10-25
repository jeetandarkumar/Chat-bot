import 'package:dartz/dartz.dart';
import 'package:chat_app/src/features/auth/domain/repositories/auth_repository.dart';
import 'package:chat_app/src/core/errors/exceptions.dart';

class LoginUseCase {
  final AuthRepository _repository;

  LoginUseCase(this._repository);

  Future<Either<AppException, AuthResult>> call(String phoneNumber) async {
    // Validate phone number format
    if (phoneNumber.trim().isEmpty) {
      return Left(const ValidationException('Phone number is required'));
    }
    
    // Basic format check - should start with + and have at least 10 digits
    final cleaned = phoneNumber.replaceAll(RegExp(r'[\s\-()]'), '');
    if (!cleaned.startsWith('+') || cleaned.length < 10) {
      return Left(const ValidationException('Invalid phone number format. Use international format (e.g., +1234567890)'));
    }

    return await _repository.login(cleaned);
  }
}