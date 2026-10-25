import 'package:dartz/dartz.dart';
import 'package:chat_app/src/features/auth/domain/repositories/auth_repository.dart';
import 'package:chat_app/src/features/auth/domain/entities/user.dart';
import 'package:chat_app/src/features/auth/data/datasources/auth_remote_datasource.dart';
import 'package:chat_app/src/core/errors/exceptions.dart';

class AuthRepositoryImpl implements AuthRepository {
  final AuthRemoteDataSource _remoteDataSource;

  AuthRepositoryImpl(this._remoteDataSource);

  @override
  Future<Either<AppException, AuthResult>> login(String phoneNumber) async {
    return await _remoteDataSource.login(phoneNumber);
  }

  @override
  Future<Either<AppException, AuthResult>> register(String phoneNumber, String? displayName) async {
    return await _remoteDataSource.register(phoneNumber, displayName);
  }

  @override
  Future<Either<AppException, AuthTokens>> verifyOtp(String phoneNumber, String code, OtpPurpose purpose) async {
    return await _remoteDataSource.verifyOtp(phoneNumber, code, purpose);
  }

  @override
  Future<Either<AppException, User>> getCurrentUser() async {
    final result = await _remoteDataSource.getCurrentUser();
    return result.fold(
      (error) => Left(error),
      (userModel) => Right(userModel.toEntity()),
    );
  }

  @override
  Future<Either<AppException, void>> logout() async {
    return await _remoteDataSource.logout();
  }

  @override
  Future<Either<AppException, AuthTokens>> refreshToken() async {
    return await _remoteDataSource.refreshToken();
  }
}