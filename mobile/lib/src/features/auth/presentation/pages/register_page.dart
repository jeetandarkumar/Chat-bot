import 'package:chat_app/src/features/auth/presentation/bloc/auth_event.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:chat_app/src/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:chat_app/src/features/auth/presentation/widgets/phone_input_field.dart';
import 'package:chat_app/src/shared/widgets/custom_button.dart';
import 'package:chat_app/src/shared/widgets/custom_text_field.dart';
import 'package:chat_app/src/core/theme/app_theme.dart';
import 'package:chat_app/src/core/config/app_routes.dart';

class RegisterPage extends StatefulWidget {
  const RegisterPage({super.key});

  @override
  State<RegisterPage> createState() => _RegisterPageState();
}

class _RegisterPageState extends State<RegisterPage> {
  final TextEditingController _displayNameController = TextEditingController();
  final TextEditingController _phoneController = TextEditingController();

  @override
  void dispose() {
    _displayNameController.dispose();
    _phoneController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: BlocListener<AuthBloc, AuthState>(
          listener: (context, state) {
            if (state.isOtpRequired) {
              Navigator.pushNamed(context, AppRoutes.otpVerification);
            } else if (state.isAuthenticated) {
              Navigator.pushNamedAndRemoveUntil(context, AppRoutes.home, (route) => false);
            } else if (state.errorMessage != null) {
              ScaffoldMessenger.of(context).showSnackBar(
                SnackBar(content: Text(state.errorMessage!)),
              );
            }
          },
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const SizedBox(height: 32),
                // Logo/Title
                Center(
                  child: Column(
                    children: [
                      Container(
                        width: 80,
                        height: 80,
                        decoration: BoxDecoration(
                          color: AppTheme.primaryColor,
                          borderRadius: BorderRadius.circular(20),
                        ),
                        child: const Icon(
                          Icons.chat_bubble_outline,
                          color: Colors.white,
                          size: 40,
                        ),
                      ),
                      const SizedBox(height: 24),
                      Text(
                        'Create Account',
                        style: Theme.of(context).textTheme.headlineMedium?.copyWith(
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      const SizedBox(height: 8),
                      Text(
                        'Enter your details to get started',
                        style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                          color: AppTheme.textSecondaryColor,
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 48),
                // Display Name Field
                CustomTextField(
                  controller: _displayNameController,
                  label: 'Display Name',
                  hintText: 'Enter your name',
                  prefixIcon: Icons.person_outline,
                ),
                const SizedBox(height: 16),
                // Phone Input
                PhoneInputField(
                  controller: _phoneController,
                  onSubmitted: (phone) {},
                ),
                const SizedBox(height: 24),
                // Register Button
                BlocBuilder<AuthBloc, AuthState>(
                  builder: (context, state) {
                    return CustomButton(
                      text: 'Register',
                      onPressed: state.isLoading
                          ? null
                          : () {
                              final phone = _phoneController.text.trim();
                              final displayName = _displayNameController.text.trim();
                              if (phone.isNotEmpty && displayName.isNotEmpty) {
                                context.read<AuthBloc>().add(
                                  AuthRegisterRequested(
                                    phoneNumber: phone,
                                    displayName: displayName,
                                  ),
                                );
                              }
                            },
                      isLoading: state.isLoading,
                    );
                  },
                ),
                const SizedBox(height: 16),
                // Login Link
                Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Text(
                      'Already have an account? ',
                      style: Theme.of(context).textTheme.bodyMedium,
                    ),
                    TextButton(
                      onPressed: () => Navigator.pushNamed(context, AppRoutes.login),
                      child: const Text('Sign In'),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}