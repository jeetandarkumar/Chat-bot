import 'package:chat_app/src/features/auth/domain/entities/user.dart' show User;
import 'package:dartz/dartz.dart';
import 'package:chat_app/src/features/auth/domain/repositories/auth_repository.dart';
import 'package:chat_app/src/core/errors/exceptions.dart';

class GetCurrentUserUseCase {
  final AuthRepository _repository;

  GetCurrentUserUseCase(this._repository);

  Future<Either<AppException, User>> call() async {
    return await _repository.getCurrentUser();
  }
}