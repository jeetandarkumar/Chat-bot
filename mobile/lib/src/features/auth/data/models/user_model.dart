import 'package:json_annotation/json_annotation.dart';
import '../../domain/entities/user.dart';

part 'user_model.g.dart';

@JsonSerializable()
class UserModel {
  final String id;
  final String phoneNumber;
  final String? displayName;
  final String? profilePictureUrl;
  final String? bio;
  final bool isOnline;
  final DateTime? lastSeen;
  final DateTime createdAt;
  final DateTime updatedAt;

  const UserModel({
    required this.id,
    required this.phoneNumber,
    this.displayName,
    this.profilePictureUrl,
    this.bio,
    this.isOnline = false,
    this.lastSeen,
    required this.createdAt,
    required this.updatedAt,
  });

  factory UserModel.fromJson(Map<String, dynamic> json) => _$UserModelFromJson(json);
  Map<String, dynamic> toJson() => _$UserModelToJson(this);

  User toEntity() => User(
        id: id,
        phoneNumber: phoneNumber,
        displayName: displayName,
        profilePictureUrl: profilePictureUrl,
        bio: bio,
        isOnline: isOnline,
        lastSeen: lastSeen,
        createdAt: createdAt,
        updatedAt: updatedAt,
      );

  static UserModel fromEntity(User user) => UserModel(
        id: user.id,
        phoneNumber: user.phoneNumber,
        displayName: user.displayName,
        profilePictureUrl: user.profilePictureUrl,
        bio: user.bio,
        isOnline: user.isOnline,
        lastSeen: user.lastSeen,
        createdAt: user.createdAt,
        updatedAt: user.updatedAt,
      );
}