# Chat Application - Project Status Report

**Last Updated**: 2025-10-25  
**Environment**: Windows (Flutter, .NET 10, SQL Server available)

---

## 0. Update 2026-10-26 (build verification)

**Corrections to this document.** The sections below predate some of the code. The repo already contains `ChatHub`, `ConversationsController`, `MessagesController` and `ContactsController`. They were written but never built or tested, so treat them as **unverified**, not complete. Mobile messaging and contacts screens still use mock data (no BLoC, domain or data layers).

**Local build results (Windows, reported by the project owner):**
- Before fixes: `dotnet build` failed with 4 x CS1061 in `ContactsController`; `flutter test` failed (stale counter test).
- After fixes: `dotnet build` succeeded with 0 warnings and 0 errors. `flutter test`: 7 tests passed. `flutter analyze`: no issues.
- `dotnet ef database update`: succeeded after adding the `AddParticipantIsDeleted` migration. Database `ChatApp` created.
- `flutter build apk --debug`: succeeded after installing NDK 28.2.13676358, setting `compileSdk = 37` and enabling core library desugaring. Output `app-debug.apk` (about 164 MB).
- Not yet verified: any end-to-end flow on a physical phone (register, OTP, login, logout, token restore).

**Fixes made (verified by a local build, see above):**
- Moved `IConversationRepository` into `Application/Interfaces`, exposed it as `IUnitOfWork.Conversations`, and `UnitOfWork` now creates `ConversationRepository`. `ContactsController` calls `GetConversationBetweenUsersAsync`.
- `backend.csproj`: removed redundant `Microsoft.AspNetCore.SignalR` reference (NU1510). Pinned `System.Security.Cryptography.Pkcs` to 10.0.13 to clear the NU1902/NU1903 advisories (NU1902/NU1903 warnings no longer appear).
- `mobile/test/widget_test.dart`: replaced the counter test with validator unit tests.

**Hub reliability changes (branch `feature/hub-reliability`, written but NOT yet built or tested):**
- New `IMessageService` / `MessageService` holds the send, delivered-on-connect and mark-read logic. `ChatHub` and `MessagesController` both use it, so the two paths no longer diverge.
- Duplicate prevention: `Message.ClientMessageId` plus a filtered unique index on (ConversationId, SenderId, ClientMessageId). A retry with the same id returns the original message (`IsDuplicate = true`). Needs a new EF migration.
- Hub `SendMessage(conversationId, content, replyToMessageId, clientMessageId)` now returns a `SendMessageAck` (success, error, messageId, status, timestamp, isDuplicate). The old `Error` event is no longer sent from it.
- Delivered status: set on send when all recipients are connected, and for all pending messages when a user connects. Senders get a `MessageStatusChanged` event `{conversationId, messageIds, status, at}`. For group chats this is approximate: per-recipient receipts are not modelled yet.
- Read status: `MarkAsRead` validates that the message is in the conversation, marks messages up to it as Read, resets the reader's unread count, and notifies senders. A `MessagesRead` event goes to the reader's other devices.
- Unread counts are now incremented for the other participants on every send (previously never). The increment is read-modify-write, so concurrent sends to one conversation can lose a count.
- Real-time delivery now goes through each participant's personal `user_{id}` group instead of the `conversation_{id}` group, so a conversation created after a user connected still reaches them live. Typing events still use conversation groups.
- `ConversationParticipant.Leave()` now sets `IsDeleted`, so users who left (or removed a contact) lose access to the conversation.
- REST `POST /messages` accepts `clientMessageId`, uses the same service and pushes `MessageReceived` to connected participants. REST send does not mark messages Delivered.
- Known gaps: `GET /messages` loads every message in the conversation into memory and does N+1 user lookups (paging should move into the query); no blocking; no backend test project.

**First runtime test (emulator) found and fixed (branch `fix/ef-query-translation`, not yet re-verified):**
- `POST /api/auth/register` returned 500: EF Core could not translate `u.PhoneNumber.Value == ...` because `PhoneNumber` is a value object stored through a value converter (`.Value` is not a column). Predicates now compare `u.PhoneNumber == phoneNumber`. Same bug in login, verify-otp, and the contacts endpoints.
- `OtpService` and the refresh-token lookup filtered on computed properties (`IsExpired`, `IsVerified`, `IsActive`), which also cannot be translated. They now use the mapped columns (`ExpiresAt`, `VerifiedAt`, `RevokedAt`).
- `PhoneNumber` regexes had doubled backslashes in verbatim strings, so `Parse` rejected every number (and EF calls `Parse` when loading users). Fixed. Auth endpoints now validate and normalize the phone number with `PhoneNumber.TryParse` and return 400 on invalid input (previously unvalidated).
- Contact search: phone numbers are now matched exactly (substring search across all users' numbers could not be translated and enumerates users); display names still match by substring.
- Lesson: no query in this codebase had been run against SQL Server before. Other predicates were reviewed; the remaining ones use mapped columns only.

**Second runtime test (emulator): register flow (branch `fix/register-race-and-logging`, not yet re-verified):**
- After the query fixes, `POST /api/auth/register` succeeded and printed the OTP. A second register request (a double tap while the first was still warming up, about 12 s on a cold start) then failed with a duplicate key on `IX_Users_PhoneNumber` and returned 500. Register is now idempotent: an existing unverified user gets a fresh OTP, and a lost insert race is caught and handled. Verified numbers still get 409.
- `POST /api/auth/verify-otp` returned 400. The log did not say why. `VerifyOtpAsync` now logs the reason (no active code, or wrong code with attempt count) and accepts a match against any active code for the number/purpose.
- OTP codes were generated with `System.Random` and always printed to the console. Now `RandomNumberGenerator`, and printed only when `Otp:Provider` is `Development`.
- Logging: removed duplicate Console/File sinks (every line was written twice), restored the "Now listening on" startup line, and HTTPS redirection is skipped in Development (no HTTPS port exists for LAN testing).
- Mobile: the register button should be disabled while the request is in flight (not yet checked).

**Android dev setup notes:** `mobile/.env` (UTF-8, gitignored) must exist before `flutter build` because it is declared as an asset. Use the PC's LAN IP in `API_BASE_URL`. The debug manifest allows cleartext HTTP; release builds do not. Windows Firewall must allow inbound TCP 5078 on the private profile.

**Still open from the review:** committed `appsettings.json` has a machine-specific connection string and a placeholder JWT secret; hub `SendMessage` has no client message ID (duplicates on retry); Delivered status is never set; `MarkAsRead` does not validate the message belongs to the conversation or update `Message.Status`; no blocking; no rate limiting on OTP endpoints.

---

## 1. Architecture Overview

### Mobile (Flutter)
- **Framework**: Flutter 3.13.7 / Dart 3.x
- **State Management**: flutter_bloc (BLoC pattern)
- **Architecture**: Clean Architecture (Presentation → Domain → Data)
- **Networking**: Dio with interceptors (auth, logging, connectivity)
- **Local Storage**: flutter_secure_storage for tokens
- **Real-time**: signalr_netcore (configured, not implemented)
- **Dependency Injection**: MultiRepositoryProvider + MultiBlocProvider
- **Navigation**: Named routes with onGenerateRoute

### Backend (ASP.NET Core)
- **Framework**: .NET 10 Web API
- **Database**: SQL Server (Entity Framework Core 10)
- **Authentication**: JWT Bearer tokens with refresh token rotation
- **Real-time**: SignalR (configured, no hubs implemented)
- **Logging**: Serilog (console + file)
- **Health Checks**: SQL Server health check
- **Architecture**: Clean Architecture (Domain → Application → Infrastructure → API)

---

## 2. Completed Features

### Mobile ✅
| Feature | Status | Notes |
|---------|--------|-------|
| **App Foundation** | Complete | Main entry, routing, DI, theming, BLoC setup |
| **Theme System** | Complete | Light/dark Material 3 themes, custom colors |
| **Auth UI - Login** | Complete | Phone input, validation, BLoC integration |
| **Auth UI - Register** | Complete | Display name + phone, validation |
| **Auth UI - OTP Verification** | Complete | 6-digit native input, resend timer, BLoC |
| **Auth BLoC** | Complete | 8 events (added AuthInitializeRequested), proper state management |
| **Auth Use Cases** | Complete | Login, Register, VerifyOTP, Logout, GetCurrentUser |
| **Auth Repository** | Complete | Interface + implementation with Either error handling |
| **Auth Remote DataSource** | Complete | Dio-based API calls with error mapping |
| **Token Persistence** | Complete | flutter_secure_storage + auto-restore on app start |
| **Settings Page** | Complete | Profile display, categorized settings, logout |
| **Chats Page** | Complete | UI with mock data, FAB |
| **Chat Page** | Complete | Message bubbles, input, scroll, dummy data |
| **Contacts Page** | Complete | UI with mock data, FAB |
| **Home Page** | Complete | Bottom navigation (3 tabs) |

### Backend ✅
| Feature | Status | Notes |
|---------|--------|-------|
| **Domain Entities** | Complete | User, Message, Conversation, Participant, OtpCode, RefreshToken |
| **Value Objects** | Complete | PhoneNumber, MessageContent |
| **DbContext & Configurations** | Complete | EF Core with soft-delete filters, all entity configs |
| **Unit of Work** | Complete | Repository per entity, transaction support |
| **Repository Pattern** | Complete | Generic IRepository + implementation |
| **JWT Token Service** | Complete | Access/refresh tokens, validation, claims |
| **OTP Service** | Complete | Generation, verification, cleanup, dev logging |
| **Configuration** | Complete | JwtSettings, OtpSettings bound to config |
| **Auth Controller** | Complete | All 6 endpoints: login, register, verify-otp, refresh, logout, me |
| **Program.cs** | Complete | DI, Auth, HealthChecks, SignalR, CORS, Serilog |
| **Database Connection** | Configured | LocalDB/TrustedConnection in appsettings.json |
| **EF Core Migration** | Complete | InitialCreate migration generated |

---

## 3. Partially Implemented Features

| Feature | Mobile | Backend | Gap |
|---------|--------|---------|-----|
| **Authentication Flow** | UI + BLoC + Repository + Token Persistence | Domain + Services + Config + **AuthController** | **Ready to test end-to-end** |
| **Real-time Messaging** | signalr_netcore dep | SignalR in DI | **No Hubs, no client integration** |
| **Conversations/Messages** | UI only (mock data) | Domain entities exist | **No API, no use cases, no repos** |
| **Contacts/Discovery** | UI only (mock data) | Domain entities exist | **No API, no use cases, no repos** |
| **Session Restoration** | **Complete** (AuthInitializeRequested) | Token validation exists | **Ready to test** |
| **Push Notifications** | firebase_messaging dep | Not started | **No FCM integration** |
| **Media/Attachments** | image_picker dep | Not started | **No upload/download API** |
| **E2E Encryption** | encrypt/pointycastle dep | Not started | **Protocol not selected/implemented** |

---

## 4. Missing Features (Not Started)

### Backend - Critical
- [ ] **Conversations API** (GET/POST conversations, participants)
- [ ] **Messages API** (GET paginated, POST send, read receipts)
- [ ] **SignalR Hub** (ChatHub with auth, join/leave groups, send/receive)
- [ ] **Contacts API** (search by phone, add/remove, sync)
- [ ] **Media API** (presigned URLs, upload/download)
- [ ] **Push Notification Service** (FCM integration)

### Mobile - Critical
- [ ] **SignalR Client Integration** (connection, reconnection, event handlers)
- [ ] **Conversation List BLoC** (load, pagination, real-time updates)
- [ ] **Chat BLoC** (send, receive, status updates, pagination)
- [ ] **Contact BLoC** (search, add, list)
- [ ] **Offline Queue** (local DB for pending messages)
- [ ] **Media Handling** (pick, upload, display, download)

### Infrastructure
- [ ] **Database Update** (EF Core migrations applied to LocalDB)
- [ ] **API Documentation** (OpenAPI/Swagger)
- [ ] **Integration Tests** (backend + mobile)
- [ ] **CI/CD Pipeline** (not configured)

---

## 5. Current Build & Test Results

### Mobile
```bash
flutter analyze        # ✅ No issues found
flutter test           # ❌ Tests reference non-existent counter app
flutter build apk      # Not tested
```

### Backend
```bash
dotnet build           # ✅ Build succeeded (6 warnings: SignalR ref, crypto vulns)
dotnet test            # No test project exists
dotnet ef migrations   # ✅ InitialCreate migration generated
dotnet ef database update  # ❌ Connection to LocalDB needs verification
```

### Database
- SQL Server: **Available** (LocalDB instance `MSSQL$SA` running)
- Database `ChatApp`: Not created (migration generated, not applied)
- Migrations: ✅ InitialCreate generated

---

## 6. Known Issues & Dependencies

### Security Warnings
- `Microsoft.AspNetCore.SignalR` package reference redundant (auto-included in .NET 10)
- `System.Security.Cryptography.Pkcs` has known vulnerabilities (transitive dependency)

### Configuration Needed
- `appsettings.json` contains placeholder JWT secret - **must be replaced for production**
- OTP Provider = "Development" (logs codes to console) - **document as dev-only**
- FCM credentials: Not configured
- Media storage: Not configured

### Code Quality
- Mobile test file tests a counter app, not actual app
- No backend unit/integration tests
- Some Flutter warnings about unused imports (non-blocking)

---

## 7. Next Recommended Implementation Step

### **Stage 3: Verify Auth End-to-End & Database Setup**

**Priority**: High | **Blocked By**: LocalDB connection | **Estimated Effort**: Low

#### Tasks:
1. **Apply EF Core Migration** to LocalDB:
   - Verify connection string works with `localhost\SA` instance
   - Run `dotnet ef database update`

2. **Test Full Auth Flow** on physical Android device:
   - Start backend (`dotnet run`)
   - Start mobile app (`flutter run`)
   - Test: Register → OTP → Login → Settings → Logout
   - Verify token persistence across app restarts

3. **Fix any integration issues** between mobile and backend

---

## 8. File Structure Reference

### Mobile Key Files
```
lib/
├── main.dart                          # App entry + dotenv + AuthInitializeRequested trigger
├── src/
│   ├── app.dart                       # MaterialApp + providers (now in main.dart)
│   ├── core/
│   │   ├── config/app_routes.dart     # Named routes
│   │   ├── config/app_config.dart     # Constants
│   │   ├── network/dio_client.dart    # Dio + interceptors + token persistence
│   │   ├── theme/app_theme.dart       # Material 3 themes
│   │   └── errors/exceptions.dart     # Exception hierarchy
│   ├── features/
│   │   ├── auth/                      # ✅ Complete (UI + BLoC + Domain + Data + Token Persistence)
│   │   ├── messaging/                 # 🟡 UI only (mock data)
│   │   ├── contacts/                  # 🟡 UI only (mock data)
│   │   ├── settings/                  # ✅ Complete (UI + AuthBloc integration)
│   │   └── home/                      # ✅ Complete (bottom nav)
│   └── shared/
│       ├── providers/app_provider.dart # DI configuration
│       └── widgets/                   # CustomButton, CustomTextField
```

### Backend Key Files
```
backend/
├── Program.cs                         # ✅ Complete DI + middleware
├── appsettings.json                   # ✅ Config (DB, JWT, OTP, Serilog)
├── Api/
│   ├── Configuration/                 # JwtSettings, OtpSettings
│   ├── Controllers/
│   │   └── AuthController.cs          # ✅ All 6 auth endpoints
│   └── Hubs/                          # ❌ EMPTY
├── Application/
│   ├── Interfaces/                    # ✅ IUnitOfWork, IRepository, IJwtToken, IOtp
│   └── Features/                      # 🟡 Directories exist, no code
├── Domain/
│   ├── Entities/                      # ✅ All 6 entities
│   ├── ValueObjects/                  # ✅ PhoneNumber, MessageContent
│   └── Events/                        # 🟡 Directory exists
└── Infrastructure/
    ├── Persistence/
    │   ├── ChatAppDbContext.cs        # ✅ DbContext + configs
    │   ├── Configurations/            # ✅ All 6 entity configs
    │   └── Repositories/              # ✅ UnitOfWork + Generic Repository
    ├── Services/
    │   ├── JwtTokenService.cs         # ✅ Complete
    │   └── OtpService.cs              # ✅ Complete (dev logs OTP)
    └── SignalR/                       # ❌ EMPTY
```

---

*This document should be updated after each significant development stage.*