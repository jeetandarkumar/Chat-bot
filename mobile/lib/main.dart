import 'package:flutter/material.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'src/features/auth/presentation/bloc/auth_bloc.dart';
import 'src/core/config/app_config.dart';
import 'src/core/config/app_routes.dart';
import 'src/core/theme/app_theme.dart';
import 'src/shared/providers/app_provider.dart';

final GlobalKey<NavigatorState> rootNavigatorKey = GlobalKey<NavigatorState>();

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await dotenv.load(fileName: '.env').catchError((_) {});
  runApp(const ChatApp());
}

class ChatApp extends StatelessWidget {
  const ChatApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MultiRepositoryProvider(
      providers: AppProviders.repositoryProviders,
      child: MultiBlocProvider(
        providers: AppProviders.blocProviders,
        child: MaterialApp(
          title: AppConfig.appName,
          debugShowCheckedModeBanner: false,
          theme: AppTheme.lightTheme,
          darkTheme: AppTheme.darkTheme,
          themeMode: ThemeMode.system,
          navigatorKey: rootNavigatorKey,
          initialRoute: AppRoutes.splash,
          routes: AppRoutes.routes,
          onGenerateRoute: AppRoutes.onGenerateRoute,
          // One place decides where the user belongs based on the session, so every
          // screen (OTP, settings/logout, an expired session) behaves the same way.
          builder: (context, child) => BlocListener<AuthBloc, AuthState>(
            listenWhen: _shouldNavigate,
            listener: _navigate,
            child: child ?? const SizedBox.shrink(),
          ),
        ),
      ),
    );
  }

  static bool _shouldNavigate(AuthState previous, AuthState current) {
    if (previous.status == current.status) return false;

    // Signed in (OTP verified, or session restored at start-up).
    if (current.isAuthenticated) return true;

    // Start-up with no valid session, or logged out / session lost while signed in.
    // Failed login/register attempts also end up "unauthenticated", but they come from
    // the loading state, so they do not move the user away from the form they are on.
    return current.status == AuthStatus.unauthenticated &&
        (previous.status == AuthStatus.initial ||
            previous.status == AuthStatus.authenticated);
  }

  static void _navigate(BuildContext context, AuthState state) {
    final route = state.isAuthenticated ? AppRoutes.home : AppRoutes.login;

    void go() {
      rootNavigatorKey.currentState?.pushNamedAndRemoveUntil(route, (_) => false);
    }

    if (rootNavigatorKey.currentState != null) {
      go();
    } else {
      // The navigator is not built yet (very fast start-up); try again after the first frame.
      WidgetsBinding.instance.addPostFrameCallback((_) => go());
    }
  }
}
