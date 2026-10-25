"# ChatApp - WhatsApp-like Chat Application

A cross-platform messaging application built with Flutter (mobile) and ASP.NET Core Web API (.NET 10).

## Architecture

```
ChatApp/
├── backend/                 # ASP.NET Core Web API (.NET 10)
│   ├── Api/                # Controllers, Hubs, Middleware, Filters
│   ├── Application/        # Use cases, Interfaces, DTOs
│   ├── Domain/             # Entities, Value Objects, Domain Events
│   └── Infrastructure/     # Persistence, Services, SignalR
│
└── mobile/                 # Flutter Application
    └── lib/src/
        ├── core/           # Config, Theme, Network, Utils, Errors
        ├── features/       # Feature modules (Auth, Messaging, Contacts, Settings)
        │   ├── auth/
        │   ├── messaging/
        │   ├── contacts/
        │   └── settings/
        └── shared/         # Shared widgets, models, services, providers
```

## Technology Stack

### Backend
- **Framework**: ASP.NET Core Web API targeting .NET 10
- **Database**: SQL Server with Entity Framework Core
- **Real-time**: SignalR
- **Authentication**: JWT with refresh token rotation
- **Validation**: FluentValidation
- **Logging**: Serilog
- **Health Checks**: ASP.NET Core Health Checks
- **Documentation**: OpenAPI/Swagger

### Mobile
- **Framework**: Flutter 3.x with Dart 3.x
- **State Management**: flutter_bloc (BLoC pattern)
- **Networking**: Dio with interceptors
- **Storage**: flutter_secure_storage
- **Real-time**: signalr_netcore
- **Local Notifications**: flutter_local_notifications
- **Push Notifications**: firebase_messaging
- **Code Generation**: freezed, json_serializable

## Features Implemented (Phase 1)

### Backend
- [x] Project structure with Clean Architecture
- [x] Entity Framework Core with SQL Server
- [x] Domain entities (User, Conversation, Message, RefreshToken, OtpCode)
- [x] Value objects (PhoneNumber, MessageContent)
- [x] Repository pattern with Unit of Work
- [x] JWT authentication with refresh tokens
- [x] OTP generation and verification service
- [x] Health check endpoint
- [x] Serilog logging configuration
- [x] CORS and SignalR setup
- [x] Configuration management (appsettings.json)

### Mobile
- [x] Clean Architecture project structure
- [x] BLoC state management
- [x] Dio HTTP client with interceptors (auth, logging, connectivity)
- [x] Secure token storage
- [x] Authentication flow (Login, Register, OTP Verification)
- [x] Theme system (Light/Dark mode with WhatsApp-like colors)
- [x] Form validation utilities
- [x] Custom UI components (Buttons, Text Fields, Phone Input)
- [x] Navigation and routing
- [x] Home page with bottom navigation
- [x] Settings page with profile and logout

## Prerequisites

- **.NET 10 SDK** (10.0.402+)
- **Flutter SDK** (3.24+)
- **Dart SDK** (3.5+)
- **SQL Server** (LocalDB, Express, or Developer edition)
- **Android Studio** / **VS Code** with Flutter extensions
- **Git**

## Getting Started

### Backend Setup

1. Navigate to backend directory:
```bash
cd backend
```

2. Update connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ChatApp;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

3. Run database migrations:
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

4. Start the backend:
```bash
dotnet run
```

The API will be available at:
- HTTP: http://localhost:5000
- HTTPS: https://localhost:7000
- Health check: https://localhost:7000/health
- Swagger UI: https://localhost:7000 (in Development)

### Mobile Setup

1. Navigate to mobile directory:
```bash
cd mobile
```

2. Install dependencies:
```bash
flutter pub get
```

3. Generate code:
```bash
flutter pub run build_runner build --delete-conflicting-outputs
```

4. Configure environment:
```bash
cp .env.example .env
# Edit .env with your API URLs
```

5. Run the app:
```bash
flutter run
```

## Project Structure Details

### Backend Domain Entities

- **User**: Phone number, display name, profile picture, status, verification
- **Conversation**: Direct/Group chats with participants
- **Message**: Text, media, location, contact messages with status tracking
- **ConversationParticipant**: User participation in conversations
- **RefreshToken**: Secure token rotation
- **OtpCode**: One-time passwords for verification

### Mobile Features

#### Authentication
- Phone number input with country code selector
- OTP verification with 6-digit PIN entry
- JWT token management with automatic refresh
- Secure storage for tokens

#### UI Components
- Custom themed buttons (Primary, Outlined, Text)
- Form fields with validation
- Phone number input with country picker
- PIN code entry fields

## Development

### Backend Commands
```bash
# Build
dotnet build

# Run tests
dotnet test

# Add migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Generate OpenAPI spec
dotnet run -- --environment Development
```

### Mobile Commands
```bash
# Get dependencies
flutter pub get

# Generate code
flutter pub run build_runner build --delete-conflicting-outputs

# Watch for changes
flutter pub run build_runner watch

# Run tests
flutter test

# Build APK
flutter build apk --release

# Build iOS
flutter build ios --release

# Analyze code
flutter analyze
```

## Configuration

### Backend (appsettings.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ChatApp;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Jwt": {
    "Issuer": "ChatApp",
    "Audience": "ChatAppUsers",
    "SecretKey": "your-super-secret-key-min-32-chars",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "Otp": {
    "ExpirationMinutes": 5,
    "Length": 6,
    "Provider": "Development"
  }
}
```

### Mobile (.env)
```env
API_BASE_URL=https://your-api-url
SIGNALR_HUB_URL=https://your-api-url/hubs/chat
FIREBASE_API_KEY=your_firebase_key
FIREBASE_PROJECT_ID=your_project_id
```

## API Endpoints

### Authentication
- `POST /api/auth/login` - Request OTP for login
- `POST /api/auth/register` - Register new user
- `POST /api/auth/verify-otp` - Verify OTP and get tokens
- `POST /api/auth/refresh` - Refresh access token
- `POST /api/auth/logout` - Logout and revoke tokens
- `GET /api/auth/me` - Get current user profile

### Health
- `GET /health` - Health check endpoint

## Security Considerations

- All passwords/tokens stored securely (BCrypt for passwords, secure storage for tokens)
- JWT tokens with short expiration (15 min) and refresh token rotation
- OTP codes expire after 5 minutes with max 3 attempts
- HTTPS enforced in production
- CORS configured for specific origins
- Input validation on all endpoints

## Next Phases

### Phase 2 - Authentication Completion
- [ ] Complete auth controller implementations
- [ ] Add password-based login option
- [ ] Implement token refresh endpoint
- [ ] Add device/session management

### Phase 3 - Messaging
- [ ] SignalR hub for real-time messaging
- [ ] Message persistence and history
- [ ] Delivery/read receipts
- [ ] Offline message sync

### Phase 4 - Additional Features
- [ ] Contact discovery by phone number
- [ ] Presence and last-seen status
- [ ] Push notifications (FCM/APNs)
- [ ] Media uploads (images, videos, files)
- [ ] End-to-end encryption (Signal Protocol)
- [ ] Group chats

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Run tests and linting
5. Submit a pull request"