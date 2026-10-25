import 'package:flutter/material.dart';
import 'package:chat_app/src/features/home/presentation/widgets/bottom_nav_bar.dart';
import 'package:chat_app/src/features/messaging/presentation/pages/chats_page.dart';
import 'package:chat_app/src/features/contacts/presentation/pages/contacts_page.dart';
import 'package:chat_app/src/features/settings/presentation/pages/settings_page.dart';

class HomePage extends StatefulWidget {
  const HomePage({super.key});

  @override
  State<HomePage> createState() => _HomePageState();
}

class _HomePageState extends State<HomePage> {
  int _currentIndex = 0;

  final List<Widget> _pages = [
    const ChatsPage(),
    const ContactsPage(),
    const SettingsPage(),
  ];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: IndexedStack(
        index: _currentIndex,
        children: _pages,
      ),
      bottomNavigationBar: BottomNavBar(
        currentIndex: _currentIndex,
        onTap: (index) => setState(() => _currentIndex = index),
      ),
    );
  }
}