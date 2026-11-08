import 'package:chat_app/src/core/errors/exceptions.dart';
import 'package:chat_app/src/core/network/dio_client.dart';
import 'package:chat_app/src/features/auth/domain/entities/user.dart';
import 'package:chat_app/src/features/auth/domain/repositories/auth_repository.dart';
import 'package:chat_app/src/features/auth/domain/usecases/get_current_user_usecase.dart';
import 'package:chat_app/src/features/auth/domain/usecases/login_usecase.dart';
import 'package:chat_app/src/features/auth/domain/usecases/logout_usecase.dart';
import 'package:chat_app/src/features/auth/domain/usecases/register_usecase.dart';
import 'package:chat_app/src/features/auth/domain/usecases/verify_otp_usecase.dart';
import 'package:chat_app/src/features/auth/presentation/bloc/auth_event.dart';
import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

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

  /// Runs once at app start: restores the session from the stored token, if any.
  Future<void> _onInitializeRequested(
    AuthInitializeRequested event,
    Emitter<AuthState> emit,
  ) async {
    final hasToken = await _dioClient.hasValidToken();
    if (!hasToken) {
      emit(const AuthState.unauthenticated());
      return;
    }

    // An expired access token is refreshed transparently by DioClient before this returns.
    final result = await _getCurrentUserUseCase();

    AppException? failure;
    User? currentUser;
    result.fold<void>(
      (error) {
        failure = error;
      },
      (user) {
        currentUser = user;
      },
    );

    final restoredUser = currentUser;
    if (restoredUser == null) {
      // Only discard the tokens when the server rejected them. If we are just offline,
      // keep them so the next start can still restore the session.
      if (failure is AuthenticationException) {
        await _dioClient.clearTokens();
      }
      emit(const AuthState.unauthenticated());
      return;
    }

    emit(AuthState(
      status: AuthStatus.authenticated,
      user: restoredUser,
      accessToken: await _dioClient.getAuthToken(),
      refreshToken: await _dioClient.getRefreshToken(),
    ));
  }

  Future<void> _onLoginRequested(
    AuthLoginRequested event,
    Emitter<AuthState> emit,
  ) async {
    if (state.isLoading) return;
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
            phoneNumber: event.phoneNumber,
            otpPurpose: authResult.otpPurpose,
            message: authResult.message,
          ));
        } else {
          // The server never issues tokens from login; an OTP is always required.
          emit(state.copyWith(
            status: AuthStatus.unauthenticated,
            errorMessage: 'Unexpected response from server. Please try again.',
          ));
        }
      },
    );
  }

  Future<void> _onRegisterRequested(
    AuthRegisterRequested event,
    Emitter<AuthState> emit,
  ) async {
    if (state.isLoading) return;
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
            phoneNumber: event.phoneNumber,
            otpPurpose: authResult.otpPurpose,
            message: authResult.message,
          ));
        } else {
          emit(state.copyWith(
            status: AuthStatus.unauthenticated,
            errorMessage: 'Unexpected response from server. Please try again.',
          ));
        }
      },
    );
  }

  Future<void> _onOtpVerificationRequested(
    AuthOtpVerificationRequested event,
    Emitter<AuthState> emit,
  ) async {
    if (state.isLoading) return;
    emit(state.copyWith(status: AuthStatus.loading));

    final result = await _verifyOtpUseCase(event.phoneNumber, event.code, event.purpose);
    result.fold(
      (error) => emit(state.copyWith(
        status: AuthStatus.otpRequired,
        errorMessage: error.message,
      )),
      (authTokens) => emit(AuthState(
        status: AuthStatus.authenticated,
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
    // Logging out always succeeds locally (tokens are cleared even if the server is unreachable).
    await _logoutUseCase();
    emit(const AuthState.unauthenticated());
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
        user: user,
      )),
    );
  }

  Future<void> _onOtpResendRequested(
    AuthOtpResendRequested event,
    Emitter<AuthState> emit,
  ) async {
    if (state.isLoading) return;
    final phone = state.phoneNumber;
    final purpose = state.otpPurpose;
    if (phone == null || purpose == null) return;

    emit(state.copyWith(status: AuthStatus.loading));
    final result = switch (purpose) {
      OtpPurpose.registration => await _registerUseCase(phone, null),
      OtpPurpose.login => await _loginUseCase(phone),
      OtpPurpose.phoneNumberChange => await _loginUseCase(phone),
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
