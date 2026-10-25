import 'package:chat_app/src/features/auth/domain/repositories/auth_repository.dart';
import 'package:equatable/equatable.dart';

abstract class AuthEvent extends Equatable {
  const AuthEvent();

  @override
  List<Object?> get props => [];
}

class AuthLoginRequested extends AuthEvent {
  final String phoneNumber;

  const AuthLoginRequested({required this.phoneNumber});

  @override
  List<Object?> get props => [phoneNumber];
}

class AuthRegisterRequested extends AuthEvent {
  final String phoneNumber;
  final String? displayName;

  const AuthRegisterRequested({required this.phoneNumber, this.displayName});

  @override
  List<Object?> get props => [phoneNumber, displayName];
}

class AuthOtpVerificationRequested extends AuthEvent {
  final String phoneNumber;
  final String code;
  final OtpPurpose purpose;

  const AuthOtpVerificationRequested({
    required this.phoneNumber,
    required this.code,
    required this.purpose,
  });

  @override
  List<Object?> get props => [phoneNumber, code, purpose];
}

class AuthLogoutRequested extends AuthEvent {
  const AuthLogoutRequested();
}

class AuthGetCurrentUserRequested extends AuthEvent {
  const AuthGetCurrentUserRequested();
}

class AuthOtpResendRequested extends AuthEvent {
  const AuthOtpResendRequested();
}

class AuthStatusChanged extends AuthEvent {
  final bool isAuthenticated;

  const AuthStatusChanged({required this.isAuthenticated});

  @override
  List<Object?> get props => [isAuthenticated];
}

class AuthInitializeRequested extends AuthEvent {
  const AuthInitializeRequested();
}
