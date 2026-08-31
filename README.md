# Chronolog

**Unity:** 6000.3.20f1  
**Platform:** Android 9.0+  
**Architecture:** Domain / Persistence / Presentation  
**Backend:** AWS API Gateway + Lambda + DynamoDB + S3

---

## Overview

Chronolog is an offline-first Android journaling app built with Unity.

Each record contains text and a photo from the camera or gallery. Records are saved locally first, then synchronised to AWS when the device is online. The app also supports editing, deletion, highlighted records and CSV export through the Android share sheet.

---

## Features

- Create records with text and a camera or gallery photo
- Edit and delete existing records
- Local JSON storage with crash-safe writes
- Local image storage
- Automatic cloud sync when connectivity returns
- Per-device record separation using Android `ANDROID_ID`
- Sync status: Synced, Syncing, Offline or Failed
- Highlight toggle stored remotely and reflected in the record UI
- CSV export with timestamps and metadata
- Android native camera, gallery and share-sheet integrations

---

## Architecture

Client-side layers:

- **Chronolog.Domain** - pure C# journal model, validation and sync state
- **Chronolog.Persistence** - JSON repository and image storage
- **Chronolog.Presentation** - uGUI screens, sync service and Android integrations

Server-side projects:

- **Chronolog.Server.Core** - API logic, validation and CSV generation
- **Chronolog.Server.Api** - AWS Lambda entry point and AWS service adapters
- **Chronolog.Server.Infrastructure** - AWS CDK infrastructure
- **Chronolog.Server.Tests** - xUnit tests

Data flow:

```text
Unity Android app → API Gateway → Lambda → DynamoDB (record metadata)
                                      └→ S3 (images)
```

---

## Structure

```text
Assets/
├── Fonts/
├── Plugins/                  Native Android integrations
├── Prefabs/                  Reusable UI views
├── Resources/                App icon assets
├── Scenes/                   Main.unity
├── Scripts/
│   ├── Runtime/
│   │   ├── Domain/
│   │   ├── Persistence/
│   │   └── Presentation/
│   └── Tests/                Unity EditMode tests
├── Settings/
└── Sprites/

Server/
├── Chronolog.Server.Api/
├── Chronolog.Server.Core/
├── Chronolog.Server.Infrastructure/
└── Chronolog.Server.Tests/
```

---

## How to Run

- Open the project in **Unity 6000.3.20f1**.
- Open `Assets/Scenes/Main.unity`.
- Enter Play Mode for Editor testing.

For Android:

- Install Android Build Support for this Unity version.
- Select Android in **File → Build Profiles**.
- Build an APK for ARM64 with minimum API level 28.

The configured backend is deployed in AWS `eu-central-1`.

---

## Tests

- Unity EditMode tests cover the domain model, local persistence, synchronisation helpers, device ID handling, image selection and CSV export.
- Server-side xUnit tests cover API validation, CRUD, CSV generation, DynamoDB pagination and CDK stack synthesis.

---

## Documentation

See [Chronolog Documentation.md](Chronolog%20Documentation.md) for the technical design, data flow, security considerations, testing and development decisions.
