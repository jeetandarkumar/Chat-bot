import 'package:chat_app/src/features/auth/data/models/user_model.dart';
import 'package:chat_app/src/features/auth/domain/repositories/auth_repository.dart';
import 'package:chat_app/src/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('AuthState', () {
    test('isLoading, isAuthenticated and isOtpRequired are derived from status', () {
      const loading = AuthState(status: AuthStatus.loading);
      expect(loading.isLoading, isTrue);
      expect(loading.isAuthenticated, isFalse);
      expect(loading.isOtpRequired, isFalse);

      const otp = AuthState(status: AuthStatus.otpRequired);
      expect(otp.isOtpRequired, isTrue);
      expect(otp.isLoading, isFalse);

      const authenticated = AuthState(status: AuthStatus.authenticated);
      expect(authenticated.isAuthenticated, isTrue);
      expect(authenticated.isOtpRequired, isFalse);
    });

    test('copyWith does not carry one-shot messages into the next state', () {
      const failed = AuthState(
        status: AuthStatus.unauthenticated,
        errorMessage: 'Invalid OTP',
        message: 'hello',
      );

      final next = failed.copyWith(status: AuthStatus.loading);

      expect(next.errorMessage, isNull);
      expect(next.message, isNull);
    });

    test('copyWith keeps the phone number and purpose needed to resend an OTP', () {
      const otp = AuthState(
        status: AuthStatus.otpRequired,
        phoneNumber: '+923001234567',
        otpPurpose: OtpPurpose.login,
      );

      final loading = otp.copyWith(status: AuthStatus.loading);

      expect(loading.phoneNumber, '+923001234567');
      expect(loading.otpPurpose, OtpPurpose.login);
    });
  });

  group('UserModel.fromJson', () {
    // The exact shape produced by AuthController.MapUserToResponse on the backend.
    test('parses the backend user payload', () {
      final user = UserModel.fromJson({
        'id': '3f2b7c9e-1d4a-4c55-9a53-0f2f0f3a9b11',
        'phoneNumber': '+923368065204',
        'displayName': 'Jatin',
        'profilePictureUrl': null,
        'bio': null,
        'isOnline': false,
        'lastSeen': null,
        'lastSeenAt': null,
        'isPhoneNumberVerified': true,
        'status': 'Offline',
        'createdAt': '2026-10-26T01:32:10.123Z',
        'updatedAt': '2026-10-26T01:34:02.456Z',
      });

      expect(user.phoneNumber, '+923368065204');
      expect(user.displayName, 'Jatin');
      expect(user.createdAt.isUtc, isTrue);
      expect(user.updatedAt.isUtc, isTrue);
    });
  });
}
