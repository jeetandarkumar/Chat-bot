import 'package:chat_app/src/core/network/dio_client.dart';
import 'package:chat_app/src/features/auth/data/datasources/auth_remote_datasource.dart';
import 'package:chat_app/src/features/auth/data/repositories/auth_repository_impl.dart';
import 'package:chat_app/src/features/auth/domain/repositories/auth_repository.dart';
import 'package:chat_app/src/features/auth/domain/usecases/login_usecase.dart';
import 'package:chat_app/src/features/auth/domain/usecases/register_usecase.dart';
import 'package:chat_app/src/features/auth/domain/usecases/verify_otp_usecase.dart';
import 'package:chat_app/src/features/auth/domain/usecases/logout_usecase.dart';
import 'package:chat_app/src/features/auth/domain/usecases/get_current_user_usecase.dart';
import 'package:chat_app/src/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

class AppProviders {
  static List<RepositoryProvider> get repositoryProviders => [
    RepositoryProvider<DioClient>(
      create: (context) {
        final dioClient = DioClient();
        dioClient.init();
        return dioClient;
      },
    ),
    RepositoryProvider<AuthRemoteDataSource>(
      create: (context) => AuthRemoteDataSourceImpl(context.read<DioClient>()),
    ),
    RepositoryProvider<AuthRepository>(
      create: (context) => AuthRepositoryImpl(context.read<AuthRemoteDataSource>()),
    ),
  ];

  static List<BlocProvider> get blocProviders => [
    BlocProvider<AuthBloc>(
      create: (context) => AuthBloc(
        LoginUseCase(context.read<AuthRepository>()),
        RegisterUseCase(context.read<AuthRepository>()),
        VerifyOtpUseCase(context.read<AuthRepository>()),
        LogoutUseCase(context.read<AuthRepository>()),
        GetCurrentUserUseCase(context.read<AuthRepository>()),
        context.read<DioClient>(),
      ),
    ),
  ];
}
