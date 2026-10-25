
import 'package:flutter/material.dart';
import 'package:chat_app/src/features/auth/presentation/pages/login_page.dart';
import 'package:chat_app/src/features/auth/presentation/pages/register_page.dart';
import 'package:chat_app/src/features/auth/presentation/pages/otp_verification_page.dart';
import 'package:chat_app/src/features/home/presentation/pages/home_page.dart';
import 'package:chat_app/src/features/messaging/presentation/pages/chat_page.dart';
import 'package:chat_app/src/features/contacts/presentation/pages/contacts_page.dart';
import 'package:chat_app/src/features/settings/presentation/pages/settings_page.dart';

class AppRoutes {
  static const String login = '/login';
  static const String register = '/register';
  static const String otpVerification = '/otp-verification';
  static const String home = '/home';
  static const String chat = '/chat';
  static const String contacts = '/contacts';
  static const String settings = '/settings';

  static Map<String, WidgetBuilder> get routes => {
        login: (context) => const LoginPage(),
        register: (context) => const RegisterPage(),
        otpVerification: (context) => const OtpVerificationPage(),
        home: (context) => const HomePage(),
        contacts: (context) => const ContactsPage(),
        settings: (context) => const SettingsPage(),
      };

  static Route<dynamic>? onGenerateRoute(RouteSettings settings) {
    switch (settings.name) {
      case chat:
        final args = settings.arguments;

        if (args is! Map<String, dynamic> ||
            args['contactId'] is! String ||
            args['contactName'] is! String) {
          return MaterialPageRoute(
            builder: (_) => const Scaffold(
              body: Center(
                child: Text('Invalid chat route arguments'),
              ),
            ),
          );
        }

        return MaterialPageRoute(
          builder: (context) => ChatPage(
            contactId: args['contactId'] as String,
            contactName: args['contactName'] as String,
            contactAvatar: args['contactAvatar'] as String?,
          ),
        );

      default:
        return null;
    }
  }
}
