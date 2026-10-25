import 'package:chat_app/src/features/auth/presentation/bloc/auth_event.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:chat_app/src/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:chat_app/src/core/theme/app_theme.dart';
import 'package:chat_app/src/shared/widgets/custom_button.dart';

class SettingsPage extends StatelessWidget {
  const SettingsPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Settings'),
      ),
      body: BlocBuilder<AuthBloc, AuthState>(
        builder: (context, state) {
          if (!state.isAuthenticated || state.user == null) {
            return const Center(child: CircularProgressIndicator());
          }

          final user = state.user!;

          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              // Profile section
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Row(
                    children: [
                      CircleAvatar(
                        radius: 30,
                        backgroundColor: AppTheme.primaryColor,
                        backgroundImage: user.profilePictureUrl != null
                            ? NetworkImage(user.profilePictureUrl!)
                            : null,
                        child: user.profilePictureUrl == null
                            ? Text(
                                user.displayName?.isNotEmpty == true
                                    ? user.displayName![0].toUpperCase()
                                    : user.phoneNumber[1].toUpperCase(),
                                style: const TextStyle(
                                  color: Colors.white,
                                  fontSize: 24,
                                  fontWeight: FontWeight.bold,
                                ),
                              )
                            : null,
                      ),
                      const SizedBox(width: 16),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              user.displayName?.isNotEmpty == true
                                  ? user.displayName!
                                  : 'Unknown User',
                              style: Theme.of(context).textTheme.titleLarge?.copyWith(
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                            const SizedBox(height: 4),
                            Text(
                              user.phoneNumber,
                              style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                                color: AppTheme.textSecondaryColor,
                              ),
                            ),
                          ],
                        ),
                      ),
                      IconButton(
                        icon: const Icon(Icons.edit),
                        onPressed: () {},
                      ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 24),

              // Account section
              _buildSectionHeader(context, 'Account'),
              _buildSettingsTile(
                context,
                icon: Icons.person_outline,
                title: 'Profile',
                subtitle: 'Edit your profile information',
                onTap: () {},
              ),
              _buildSettingsTile(
                context,
                icon: Icons.lock_outline,
                title: 'Privacy',
                subtitle: 'Manage your privacy settings',
                onTap: () {},
              ),
              _buildSettingsTile(
                context,
                icon: Icons.security_outlined,
                title: 'Security',
                subtitle: 'Two-factor authentication, sessions',
                onTap: () {},
              ),
              _buildSettingsTile(
                context,
                icon: Icons.notifications_outlined,
                title: 'Notifications',
                subtitle: 'Customize notification preferences',
                onTap: () {},
              ),
              const SizedBox(height: 24),

              // Chat section
              _buildSectionHeader(context, 'Chats'),
              _buildSettingsTile(
                context,
                icon: Icons.wallpaper_outlined,
                title: 'Wallpaper',
                subtitle: 'Change chat background',
                onTap: () {},
              ),
              _buildSettingsTile(
                context,
                icon: Icons.font_download_outlined,
                title: 'Font Size',
                subtitle: 'Adjust text size in chats',
                onTap: () {},
              ),
              _buildSettingsTile(
                context,
                icon: Icons.archive_outlined,
                title: 'Archived Chats',
                subtitle: 'View archived conversations',
                onTap: () {},
              ),
              _buildSettingsTile(
                context,
                icon: Icons.backup_outlined,
                title: 'Chat Backup',
                subtitle: 'Backup and restore chats',
                onTap: () {},
              ),
              const SizedBox(height: 24),

              // Data section
              _buildSectionHeader(context, 'Data & Storage'),
              _buildSettingsTile(
                context,
                icon: Icons.storage_outlined,
                title: 'Storage Usage',
                subtitle: 'Manage storage and clear cache',
                onTap: () {},
              ),
              _buildSettingsTile(
                context,
                icon: Icons.download_outlined,
                title: 'Media Auto-Download',
                subtitle: 'Configure auto-download settings',
                onTap: () {},
              ),
              const SizedBox(height: 24),

              // About section
              _buildSectionHeader(context, 'About'),
              _buildSettingsTile(
                context,
                icon: Icons.info_outline,
                title: 'App Version',
                subtitle: '1.0.0',
                onTap: () {},
              ),
              _buildSettingsTile(
                context,
                icon: Icons.help_outline,
                title: 'Help Center',
                subtitle: 'FAQs and support',
                onTap: () {},
              ),
              _buildSettingsTile(
                context,
                icon: Icons.privacy_tip_outlined,
                title: 'Privacy Policy',
                subtitle: 'Read our privacy policy',
                onTap: () {},
              ),
              _buildSettingsTile(
                context,
                icon: Icons.description_outlined,
                title: 'Terms of Service',
                subtitle: 'Read our terms of service',
                onTap: () {},
              ),
              const SizedBox(height: 32),

              // Logout button
              CustomButton(
                text: 'Log Out',
                onPressed: () {
                  context.read<AuthBloc>().add(const AuthLogoutRequested());
                },
                backgroundColor: AppTheme.errorColor,
                icon: Icons.logout,
              ),
              const SizedBox(height: 16),
            ],
          );
        },
      ),
    );
  }

  Widget _buildSectionHeader(BuildContext context, String title) {
    return Padding(
      padding: const EdgeInsets.only(left: 4, bottom: 8),
      child: Text(
        title,
        style: Theme.of(context).textTheme.labelLarge?.copyWith(
          color: AppTheme.textSecondaryColor,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }

  Widget _buildSettingsTile(
    BuildContext context, {
    required IconData icon,
    required String title,
    required String subtitle,
    required VoidCallback onTap,
  }) {
    return Card(
      margin: const EdgeInsets.only(bottom: 4),
      child: ListTile(
        leading: Container(
          padding: const EdgeInsets.all(8),
          decoration: BoxDecoration(
            color: AppTheme.primaryColor.withValues(alpha: 0.1),
            borderRadius: BorderRadius.circular(8),
          ),
          child: Icon(icon, color: AppTheme.primaryColor, size: 22),
        ),
        title: Text(
          title,
          style: Theme.of(context).textTheme.titleMedium?.copyWith(
            fontWeight: FontWeight.w500,
          ),
        ),
        subtitle: Text(
          subtitle,
          style: Theme.of(context).textTheme.bodySmall?.copyWith(
            color: AppTheme.textSecondaryColor,
          ),
        ),
        trailing: const Icon(Icons.chevron_right),
        onTap: onTap,
      ),
    );
  }
}
