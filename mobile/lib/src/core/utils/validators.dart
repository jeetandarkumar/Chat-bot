class Validators {
  static String? validatePhoneNumber(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'Phone number is required';
    }

    final cleaned = value.replaceAll(RegExp(r'[\s\-()]'), '');
    if (!cleaned.startsWith('+') || cleaned.length < 10) {
      return 'Invalid phone number format';
    }

    // E.164 format validation
    if (!RegExp(r'^\+[1-9]\d{1,14}$').hasMatch(cleaned)) {
      return 'Invalid phone number format';
    }

    return null;
  }

  static String? validateOtp(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'OTP code is required';
    }

    if (value.length != 6 || !RegExp(r'^\d{6}$').hasMatch(value)) {
      return 'Invalid OTP format. Must be 6 digits.';
    }

    return null;
  }

  static String? validateDisplayName(String? value) {
    if (value != null && value.trim().isEmpty) {
      return 'Display name cannot be empty';
    }

    if (value != null && value.trim().length > 100) {
      return 'Display name must be less than 100 characters';
    }

    return null;
  }

  static String? validateEmail(String? value) {
    if (value == null || value.trim().isEmpty) {
      return null; // Email is optional
    }

    if (!RegExp(r'^[^@]+@[^@]+\.[^@]+$').hasMatch(value.trim())) {
      return 'Invalid email format';
    }

    return null;
  }

  static String? validateRequired(String? value, String fieldName) {
    if (value == null || value.trim().isEmpty) {
      return '$fieldName is required';
    }
    return null;
  }

  static String? validateMinLength(String? value, int minLength, String fieldName) {
    if (value != null && value.length < minLength) {
      return '$fieldName must be at least $minLength characters';
    }
    return null;
  }

  static String? validateMaxLength(String? value, int maxLength, String fieldName) {
    if (value != null && value.length > maxLength) {
      return '$fieldName must be less than $maxLength characters';
    }
    return null;
  }
}