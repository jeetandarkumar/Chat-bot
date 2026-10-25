// 1. Import for `DioClient` and `dartz`
// 2. A new dependency `DioClient _dioClient` in the constructor
// 3. A new event handler for `AuthInitializeRequested`
// 4. Registration of the new event handler in the constructor

// I need to output the complete modified file with these changes applied.```
import 'package:chat_app/src/features/auth/presentation/bloc/auth_event.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:chat_app/src/features/auth/domain/usecases/login_usecase.dart';
import 'package:chat_app/src/features/auth/domain/usecases/register_usecase.dart';
import 'package:chat_app/src/features/auth/domain/usecases/verify_otp_usecase.dart';
import 'package:chat_app/src/features/auth/domain/usecases/logout_usecase.dart';
import 'package:chat_app/src/features/auth/domain/usecases/get_current_user_usecase.dart';
import 'package:chat_app/src/features/auth/domain/repositories/auth_repository.dart';
import 'package:chat_app/src/features/auth/domain/entities/user.dart';
import 'package:chat_app/src/core/network/dio_client.dart';

part 'auth_state.dart';

class AuthBloc extends Bloc<AuthEvent, AuthState> {
  final LoginUseCase _loginUseCase;
  final RegisterUseCase _registerUseCase;
  final VerifyOtpUseCase _verifyOtpUseCase;
  final LogoutUseCase _logoutUseCase;
  final GetCurrentUserUseCase _getCurrentUserUseCase;
  final DioClient _dioClient;

  AuthBloc(
    this._loginUseCase,
    this._registerUseCase,
    this._verifyOtpUseCase,
    this._logoutUseCase,
    this._getCurrentUserUseCase,
    this._dioClient,
  ) : super(const AuthState.initial()) {
    on<AuthLoginRequested>(_onLoginRequested);
    on<AuthRegisterRequested>(_onRegisterRequested);
    on<AuthOtpVerificationRequested>(_onOtpVerificationRequested);
    on<AuthLogoutRequested>(_onLogoutRequested);
    on<AuthGetCurrentUserRequested>(_onGetCurrentUserRequested);
    on<AuthOtpResendRequested>(_onOtpResendRequested);
    on<AuthStatusChanged>(_onAuthStatusChanged);
    on<AuthInitializeRequested>(_onInitializeRequested);
  }

  Future<void> _onInitializeRequested(
    AuthInitializeRequested event,
    Emitter<AuthState> emit,
  ) async {
    final hasToken = await _dioClient.hasValidToken();
    if (hasToken) {
      final accessToken = await _dioClient.getAuthToken();
      final refreshToken = await _dioClient.getRefreshToken();

      if (accessToken != null && refreshToken != null) {
        // Try to get current user with stored token
    final result = await _getCurrentUserUseCase();
    result.fold(
          (error) async {
            // Token might be expired, try to refresh
            // We'll implement refresh token logic here
            emit(const AuthState.unauthenticated());
          },
      (user) => emit(state.copyWith(
        status: AuthStatus.authenticated,
        isAuthenticated: true,
        user: user,
            accessToken: accessToken,
            refreshToken: refreshToken,
      )),
    );
    } else {
      emit(const AuthState.unauthenticated());
    }
    } else {
      emit(const AuthState.unauthenticated());
  }
}

  Future<void> _onLoginRequested(
    AuthLoginRequested event,
    Emitter<AuthState> emit,
  ) async {
    emit(state.copyWith(status: AuthStatus.loading));
    final result = await _loginUseCase(event.phoneNumber);
    result.fold(
      (error) => emit(state.copyWith(
        status: AuthStatus.unauthenticated,
        errorMessage: error.message,
      )),
      (authResult) {
        if (authResult.requiresOtp) {
          emit(state.copyWith(
            status: AuthStatus.otpRequired,
            isOtpRequired: true,
            phoneNumber: event.phoneNumber,
            otpPurpose: authResult.otpPurpose,
            message: authResult.message,
          ));
        } else {
          emit(state.copyWith(
            status: AuthStatus.authenticated,
            message: authResult.message,
          ));
          add(const AuthGetCurrentUserRequested());
        }
      },
    );
  }

  Future<void> _onRegisterRequested(
    AuthRegisterRequested event,
    Emitter<AuthState> emit,
  ) async {
    emit(state.copyWith(status: AuthStatus.loading));
    final result = await _registerUseCase(event.phoneNumber, event.displayName);
    result.fold(
      (error) => emit(state.copyWith(
        status: AuthStatus.unauthenticated,
        errorMessage: error.message,
      )),
      (authResult) {
        if (authResult.requiresOtp) {
          emit(state.copyWith(
            status: AuthStatus.otpRequired,
            isOtpRequired: true,
            phoneNumber: event.phoneNumber,
            otpPurpose: authResult.otpPurpose,
            message: authResult.message,
          ));
        } else {
          emit(state.copyWith(
            status: AuthStatus.authenticated,
            message: authResult.message,
          ));
          add(const AuthGetCurrentUserRequested());
        }
      },
    );
  }

  Future<void> _onOtpVerificationRequested(
    AuthOtpVerificationRequested event,
    Emitter<AuthState> emit,
  ) async {
    emit(state.copyWith(status: AuthStatus.loading, isLoading: true));
    final result = await _verifyOtpUseCase(event.phoneNumber, event.code, event.purpose);
    result.fold(
      (error) => emit(state.copyWith(
        status: AuthStatus.otpRequired,
        isLoading: false,
        errorMessage: error.message,
      )),
      (authTokens) => emit(state.copyWith(
        status: AuthStatus.authenticated,
        isAuthenticated: true,
        isLoading: false,
        user: authTokens.user,
        accessToken: authTokens.accessToken,
        refreshToken: authTokens.refreshToken,
        message: 'Successfully verified',
      )),
    );
  }

  Future<void> _onLogoutRequested(
    AuthLogoutRequested event,
    Emitter<AuthState> emit,
  ) async {
    emit(state.copyWith(status: AuthStatus.loading));
    final result = await _logoutUseCase();
    result.fold(
      (error) => emit(state.copyWith(
        status: AuthStatus.authenticated,
        errorMessage: error.message,
      )),
      (_) => emit(const AuthState.unauthenticated()),
    );
  }

  Future<void> _onGetCurrentUserRequested(
    AuthGetCurrentUserRequested event,
    Emitter<AuthState> emit,
  ) async {
    final result = await _getCurrentUserUseCase();
    result.fold(
      (error) => emit(state.copyWith(
        status: AuthStatus.unauthenticated,
        errorMessage: error.message,
      )),
      (user) => emit(state.copyWith(
        status: AuthStatus.authenticated,
        isAuthenticated: true,
        user: user,
      )),
    );
  }

  Future<void> _onOtpResendRequested(
    AuthOtpResendRequested event,
    Emitter<AuthState> emit,
  ) async {
    if (state.phoneNumber == null || state.otpPurpose == null) return;

    emit(state.copyWith(status: AuthStatus.loading));
    final result = switch (state.otpPurpose!) {
      OtpPurpose.registration => await _registerUseCase(
          state.phoneNumber!,
          null,
        ),
      OtpPurpose.login => await _loginUseCase(state.phoneNumber!),
      OtpPurpose.phoneNumberChange => await _loginUseCase(state.phoneNumber!),
    };

    result.fold(
      (error) => emit(state.copyWith(
        status: AuthStatus.otpRequired,
        errorMessage: error.message,
      )),
      (authResult) => emit(state.copyWith(
        status: AuthStatus.otpRequired,
        message: authResult.message ?? 'OTP resent',
      )),
    );
  }

  Future<void> _onAuthStatusChanged(
    AuthStatusChanged event,
    Emitter<AuthState> emit,
  ) async {
    if (event.isAuthenticated) {
      add(const AuthGetCurrentUserRequested());
    } else {
      emit(const AuthState.unauthenticated());
    }
  }
}
