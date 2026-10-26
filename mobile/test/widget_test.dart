import 'package:chat_app/src/core/utils/validators.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('Validators.validatePhoneNumber', () {
    test('rejects empty input', () {
      expect(Validators.validatePhoneNumber(''), isNotNull);
      expect(Validators.validatePhoneNumber(null), isNotNull);
    });

    test('rejects numbers without a leading +', () {
      expect(Validators.validatePhoneNumber('923001234567'), isNotNull);
    });

    test('accepts E.164 numbers, ignoring spaces and dashes', () {
      expect(Validators.validatePhoneNumber('+92 300-1234567'), isNull);
    });
  });

  group('Validators.validateOtp', () {
    test('accepts exactly 6 digits', () {
      expect(Validators.validateOtp('123456'), isNull);
    });

    test('rejects wrong length or non-digits', () {
      expect(Validators.validateOtp('12345'), isNotNull);
      expect(Validators.validateOtp('12345a'), isNotNull);
      expect(Validators.validateOtp(''), isNotNull);
    });
  });

  group('Validators.validateDisplayName', () {
    test('accepts a normal name', () {
      expect(Validators.validateDisplayName('Jatin'), isNull);
    });

    test('rejects blank and over-long names', () {
      expect(Validators.validateDisplayName('   '), isNotNull);
      expect(Validators.validateDisplayName('a' * 101), isNotNull);
    });
  });
}
