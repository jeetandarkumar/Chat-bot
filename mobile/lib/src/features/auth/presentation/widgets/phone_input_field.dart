import 'package:flutter/material.dart';
import 'package:chat_app/src/shared/widgets/custom_text_field.dart';

class PhoneInputField extends StatelessWidget {
  final TextEditingController controller;
  final ValueChanged<String>? onPhoneChanged;
  final ValueChanged<String>? onSubmitted;

  const PhoneInputField({
    super.key,
    required this.controller,
    this.onPhoneChanged,
    this.onSubmitted,
  });

  @override
  Widget build(BuildContext context) {
    return CustomTextField(
      controller: controller,
      label: 'Phone Number',
      hintText: '+1 555 123 4567',
      prefixIcon: Icons.phone_outlined,
      keyboardType: TextInputType.phone,
      textInputAction: TextInputAction.next,
      onFieldSubmitted: onSubmitted,
    );
  }
}