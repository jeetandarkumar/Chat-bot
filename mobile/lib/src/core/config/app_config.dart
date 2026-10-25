import 'package:flutter_dotenv/flutter_dotenv.dart';

class AppConfig {
  static const String appName = 'ChatApp';
  static const String appVersion = '1.0.0';
  
  static String get baseUrl => dotenv.env['API_BASE_URL'] ?? 'https://localhost:7000';
  static String get signalRHubUrl => dotenv.env['SIGNALR_HUB_URL'] ?? 'https://localhost:7000/hubs/chat';
  static String get firebaseApiKey => dotenv.env['FIREBASE_API_KEY'] ?? '';
  static String get firebaseProjectId => dotenv.env['FIREBASE_PROJECT_ID'] ?? '';
  
  static const int connectTimeout = 30000; // 30 seconds
  static const int receiveTimeout = 30000;
  static const int sendTimeout = 30000;
  
  static const int maxMessageLength = 4096;
  static const int maxFileSize = 50 * 1024 * 1024; // 50 MB
}