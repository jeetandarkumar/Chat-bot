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
  final bool isAuthenticated;
  final bool isLoading;
  final bool isOtpRequired;
  final User? user;
  final String? accessToken;
  final String? refreshToken;
  final String? phoneNumber;
  final OtpPurpose? otpPurpose;
  final String? message;
  final String? errorMessage;

  const AuthState({
    this.status = AuthStatus.initial,
    this.isAuthenticated = false,
    this.isLoading = false,
    this.isOtpRequired = false,
    this.user,
    this.accessToken,
    this.refreshToken,
    this.phoneNumber,
    this.otpPurpose,
    this.message,
    this.errorMessage,
  });

  const AuthState.initial()
    : this(status: AuthStatus.initial);

  const AuthState.unauthenticated()
    : this(status: AuthStatus.unauthenticated);

  AuthState copyWith({
    AuthStatus? status,
    bool? isAuthenticated,
    bool? isLoading,
    bool? isOtpRequired,
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
      isAuthenticated: isAuthenticated ?? this.isAuthenticated,
      isLoading: isLoading ?? this.isLoading,
      isOtpRequired: isOtpRequired ?? this.isOtpRequired,
      user: user ?? this.user,
      accessToken: accessToken ?? this.accessToken,
      refreshToken: refreshToken ?? this.refreshToken,
      phoneNumber: phoneNumber ?? this.phoneNumber,
      otpPurpose: otpPurpose ?? this.otpPurpose,
      message: message ?? this.message,
      errorMessage: errorMessage ?? this.errorMessage,
    );
  }

  @override
  List<Object?> get props => [
    status,
    isAuthenticated,
    isLoading,
    isOtpRequired,
    user,
    accessToken,
    refreshToken,
    phoneNumber,
    otpPurpose,
    message,
    errorMessage,
  ];
}