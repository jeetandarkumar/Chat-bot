part of 'auth_bloc.dart';

enum AuthStatus {
  initial,
  loading,
  unauthenticated,
  otpRequired,
  authenticated,
  error,
}

extension AuthStatusX on AuthStatus {
  bool get isInitial => this == AuthStatus.initial;
  bool get isLoading => this == AuthStatus.loading;
  bool get isUnauthenticated => this == AuthStatus.unauthenticated;
  bool get isOtpRequired => this == AuthStatus.otpRequired;
  bool get isAuthenticated => this == AuthStatus.authenticated;
  bool get isError => this == AuthStatus.error;
}

class AuthState extends Equatable {
  final AuthStatus status;
  final User? user;
  final String? accessToken;
  final String? refreshToken;
  final String? phoneNumber;
  final OtpPurpose? otpPurpose;

  /// One-shot messages for the UI. They are NOT carried over by [copyWith], so a
  /// stale error never reappears on the next state change.
  final String? message;
  final String? errorMessage;

  const AuthState({
    this.status = AuthStatus.initial,
    this.user,
    this.accessToken,
    this.refreshToken,
    this.phoneNumber,
    this.otpPurpose,
    this.message,
    this.errorMessage,
  });

  const AuthState.initial() : this(status: AuthStatus.initial);

  const AuthState.unauthenticated() : this(status: AuthStatus.unauthenticated);

  // Derived from [status] so they can never get out of sync with it.
  bool get isLoading => status == AuthStatus.loading;
  bool get isAuthenticated => status == AuthStatus.authenticated;
  bool get isOtpRequired => status == AuthStatus.otpRequired;

  AuthState copyWith({
    AuthStatus? status,
    User? user,
    String? accessToken,
    String? refreshToken,
    String? phoneNumber,
    OtpPurpose? otpPurpose,
    String? message,
    String? errorMessage,
  }) {
    return AuthState(
      status: status ?? this.status,
      user: user ?? this.user,
      accessToken: accessToken ?? this.accessToken,
      refreshToken: refreshToken ?? this.refreshToken,
      phoneNumber: phoneNumber ?? this.phoneNumber,
      otpPurpose: otpPurpose ?? this.otpPurpose,
      message: message,
      errorMessage: errorMessage,
    );
  }

  @override
  List<Object?> get props => [
    status,
    user,
    accessToken,
    refreshToken,
    phoneNumber,
    otpPurpose,
    message,
    errorMessage,
  ];
}
