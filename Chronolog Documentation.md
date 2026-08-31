# Chronolog - Journal App with Cloud Sync

## Delivery

- **Initial effort estimate, before development:** 2.5-3 working days (approximately 20-24 hours).
- **Android build:** APK for Android 9.0 and later (minimum SDK 28), IL2CPP and ARM64, provided through a Google Drive link.
- **Source code:** provided separately as a private Git repository link.
- **Documentation:** provided through a Google Drive link.
- **Remote server access:** AWS access is provided separately through AWS IAM Identity Center. No credentials or secrets are stored in this repository.

## 1. Overview & Definition

### 1.1 Goal / Description

Chronolog is an Android journaling app built with Unity. Each record has a text note and a photo, taken with the camera or selected from the gallery. Data is saved locally in JSON and synchronised to AWS automatically when the device is online. Records can also be exported as CSV.

The main idea is simple: write down what happened, attach a photo and do not worry about losing it when the device is temporarily offline. Records can be created, edited and deleted without an internet connection. The app synchronises them once connectivity returns.

The backend runs on AWS. One Lambda handles the API routes, DynamoDB stores record metadata and S3 stores images. The infrastructure is described using AWS CDK in C#, so it can be recreated from code.

### 1.2 Focus / User Need

The user needs a lightweight personal journal that works without a network connection but retains a cloud-backed copy of the records. The user can always see the current sync state at the bottom of the list screen: **Synced**, **Syncing**, **Offline** or **Failed**.

### 1.3 Requirements

- Unity Android app with minimum SDK 28 (Android 9.0).
- Journal records with free text and a photo from the camera or gallery.
- Local JSON storage and local image storage.
- Remote storage on AWS through API Gateway, Lambda, DynamoDB and S3.
- Create, edit and delete records.
- CSV export with timestamps and metadata, shared through the Android share sheet.
- UI driven by remote data: a synced `isHighlighted` value changes the record card background and shows a highlight icon in its top-right corner.
- Offline-first behaviour with background sync once the device is online.
- Per-device record separation using Android `ANDROID_ID`.

## 2. Risk Analysis

### 2.1 Security Risk Analysis

- **No authentication - High for a real product:** the prototype API is open and does not identify an authenticated person. `ANDROID_ID` scopes normal requests to one device, but it is not an access-control mechanism or a secret. A production version should use Cognito or another authenticated identity provider and authorise every request by user ID.
- **Device ID as an identifier - Low for the prototype:** `ANDROID_ID` is stable for an app signing key on a device and prevents test devices from mixing their records. The server validates its format. If it is unavailable, the app falls back to a random GUID stored in `PlayerPrefs`.
- **Presigned S3 URLs - Low:** uploads use presigned PUT URLs with a five-minute expiry. The S3 bucket blocks all public access. Only JPEG and PNG uploads are accepted.
- **Local data not encrypted by the app - Low for the prototype:** JSON and images are stored in `persistentDataPath`. The app relies on Android's file-based encryption and application sandboxing; a production privacy-sensitive version should consider application-level encryption.
- **Input validation - Low:** the server validates IDs, ISO 8601 dates, non-empty content, image content types and expected image-key formats. Invalid requests receive a clear HTTP 400 response.

## 3. Technical Design

### 3.1 System Architecture & Data Flow

```text
Unity Android app → API Gateway → Lambda → DynamoDB (record metadata)
                                      └→ S3 (images)
```

The Unity client has three layers with strict dependency direction:

- **Chronolog.Domain:** pure C# without Unity dependencies. Contains `JournalRecord`, validation and enums.
- **Chronolog.Persistence:** JSON repository and image storage. Depends on Domain.
- **Chronolog.Presentation:** uGUI screens, synchronisation, network monitoring and Android integrations. Depends on Domain and Persistence.

The server is a .NET 8 solution with four projects:

- **Chronolog.Server.Core:** business logic, API routing, validation and CSV generation. No AWS SDK dependency.
- **Chronolog.Server.Api:** Lambda entry point. Maps API Gateway events to Core requests.
- **Chronolog.Server.Infrastructure:** CDK stack defining DynamoDB, S3, Lambda and HTTP API resources.
- **Chronolog.Server.Tests:** xUnit tests.

#### Creating or updating a record

1. The user enters text and takes or selects a photo.
2. The photo is copied to local storage using a GUID-based filename.
3. A `JournalRecord` is created or updated with a pending sync state.
4. The record is saved to the local JSON file using a temporary file and `File.Replace` for crash safety.
5. `JournalSyncService` requests a presigned upload URL, uploads the image to S3 when necessary, saves metadata to DynamoDB and updates the local sync state.

#### Synchronisation on app start or network recovery

1. The app checks connectivity. If it is offline, it sets the status to **Offline** and stops.
2. All pending local records are sent to the server.
3. The app fetches the current record list with `GET /records`.
4. Records missing locally are restored, including downloading their image where needed.
5. Existing records receive refreshed remote metadata, including `isHighlighted`.

### 3.2 Design Specification - Fixed vs Flexible Requirements

- **JSON local storage:** fixed for this prototype. SQLite is a possible future replacement if local data grows.
- **One photo per record:** fixed. Multiple photos can be added later.
- **CSV export:** fixed and RFC 4180 compatible. PDF or JSON export are possible extensions.
- **CDK and one Lambda:** fixed for the current scope. Routes can be split into multiple Lambdas if the API grows.
- **uGUI:** fixed. UI Toolkit remains an option, but uGUI is more familiar and mature for this mobile prototype.
- **Coroutines for networking:** fixed. `UnityWebRequest` is coroutine-native and avoids potential IL2CPP/Android async edge cases.
- **`isHighlighted` for database-driven UI:** fixed. The value is stored remotely and changes a record card after sync. Other server-driven visual options can be added later.

### 3.3 Backend Components

API endpoints:

- `GET /records` - lists records for the current device.
- `POST /records` - creates a record.
- `PUT /records/{id}` - updates a record.
- `DELETE /records/{id}` - removes a record. DynamoDB is deleted first, then S3.
- `GET /records/{id}/image` - returns a presigned image download URL.
- `POST /uploads` - requests a presigned upload URL.
- `GET /export.csv` - exports all records for the current device as CSV.

**DynamoDB** uses one table with `deviceId` as the partition key and record UUID as the sort key. This naturally scopes queries to a single device. Billing mode is `PAY_PER_REQUEST`.

**S3** stores files under `images/{deviceId}/{recordId}.{ext}`. Public access is blocked and all image operations use presigned URLs with a five-minute expiry. Updating an image removes the previous S3 object. For deletion, DynamoDB is removed first: an orphaned file is preferable to a journal record that points to a missing image.

`DynamoDbQueryPager` follows DynamoDB `LastEvaluatedKey` values, so record listing does not silently stop after the first response page.

### 3.4 Frontend Components

The uGUI interface has two screens, switched by `JournalScreenNavigator`:

- **List screen:** a scrollable list with thumbnails, dates, record text and photo source (Camera or Gallery). Highlighted records use a different background. The list shows a loading animation during sync and the empty state appears only after sync finishes. The status bar at the bottom has a colour-coded icon.
- **Form screen:** used for both new and existing records. It contains image preview, camera/gallery buttons, text field, Highlight toggle, Save and Cancel buttons. Editing also provides Delete with a confirmation popup.

Key client components:

- `JournalSyncService` performs the full sync cycle and raises `SyncCompleted` and `StatusChanged` events.
- `JournalNetworkMonitor` polls `Application.internetReachability` every two seconds and retries sync after a connection returns.
- `JournalSyncStatusBar` renders the current sync state with a colour-coded icon.
- `LoadingAnimation` renders the sync loading state.
- `JournalCsvExporter` downloads CSV, writes it to a temporary file and shares it through `NativeShare`.
- `JournalDeviceId` reads Android `ANDROID_ID` through JNI and uses a PlayerPrefs GUID fallback in the Editor.
- `JournalKeyboardController` reads the real Android soft-keyboard frame through JNI so the input field remains visible.

### 3.5 Outside Tools or Plugins Used & Licenses

- NativeCamera by yasirkula - MIT.
- NativeGallery by yasirkula - MIT.
- NativeShare by yasirkula - MIT.
- AWS CDK v2.266.0 - Apache-2.0.
- AWS SDK for .NET (DynamoDB and S3) - Apache-2.0.
- Amazon.Lambda packages - Apache-2.0.
- xUnit v2.9.2 - Apache-2.0.
- Unity Test Framework v1.6.0 - Unity Companion License.

### 3.6 Security Architecture & Controls

- Every request includes the `x-chronolog-device-id` header. DynamoDB queries are scoped by it.
- The server uses `System.Text.Json` deserialisation inside error handling and validates every incoming request.
- S3 blocks all public access; uploads and downloads use short-lived presigned URLs.
- CORS allows all origins for the mobile prototype, while methods and headers are restricted.
- A top-level API error handler logs unexpected exceptions on the server and returns a generic HTTP 500 response to the client.
- JSON writes use a temporary-file pattern with `File.Replace` to reduce corruption risk after interruption.

### 3.7 Failure & Edge Considerations

- **Network lost during sync:** connection errors are detected per request, status changes to Offline, records remain pending and sync retries after connectivity returns.
- **App crash during a local write:** the temporary-file pattern preserves the previous JSON document. A leftover temp file is cleaned on a later write.
- **S3 upload succeeds but DynamoDB save fails:** an orphaned S3 file may remain. The local record stays pending and is retried.
- **DynamoDB deletion succeeds but S3 deletion fails:** an orphaned S3 file may remain, but no record points to a missing image.
- **Local image file is missing:** the record is marked Failed with a descriptive error.
- **Malformed remote record:** invalid data is skipped during restore and logged with `Debug.LogWarning`.
- **Large record sets:** DynamoDB query pagination is handled server-side. Client-side list pagination is a future improvement.

## 4. Design Review - Questions & Decisions

- **CDK or SAM?** CDK was selected because the stack is C#, the same language as the Lambda. It is easier to maintain than an additional YAML infrastructure format.
- **One Lambda or one per route?** One Lambda is sufficient for the small API and makes deployment simpler. Cold-start cost is acceptable for this prototype.
- **Coroutines or async/await in Unity?** Coroutines were selected because `UnityWebRequest` is coroutine-native and Android IL2CPP async behaviour can require more care.
- **JsonUtility or Newtonsoft on the client?** `JsonUtility` was selected because it is fast and IL2CPP-friendly. The server uses built-in `System.Text.Json`.
- **How should database data change the UI?** The synced `isHighlighted` value changes a record card background. The user controls it through the Highlight toggle; future server-side rules could set it automatically.
- **Upload through Lambda or directly to S3?** Direct S3 upload through presigned URLs avoids Lambda payload limits and reduces execution time.
- **Delete DynamoDB or S3 first?** DynamoDB first. A harmless orphaned S3 file is preferable to a visible journal record with a broken image.
- **Should New record be blocked during sync?** No. A record is stored locally immediately, so blocking creation would be poor offline-first UX.
- **Should the list scroll during sync?** No. The list is being rebuilt and scrolling while records appear or disappear feels unstable.

## 5. Verification & Testing Plan

### 5.1 Items and Cases to Test in QA

Automated client-side EditMode tests use 17 files:

- `JournalRecordTests` - domain model, validation and state transitions.
- `JsonJournalRecordRepositoryTests` - JSON save, read, upsert, delete and crash-safe writes.
- `JournalImageStorageTests` - image file operations.
- `JournalRecordFormDataTests` and `JournalRecordFormScreenTests` - form state, save flow, editing and delete confirmation.
- `JournalScreenNavigatorTests` and `JournalListDataTests` - navigation and list data.
- `JournalListScrollAvailabilityTests` - scroll availability by sync status.
- `JournalNetworkMonitorTests` and `JournalDeviceIdTests` - reachability and device ID generation.
- `JournalImageSelectionHandlerTests` and `JournalImageContentTypeTests` - camera/gallery flow and MIME mapping.
- `JournalKeyboardControllerTests` - keyboard handling.
- `JournalCsvExport*Tests` (four files) - CSV output, filename, availability and export button behaviour.

Server-side xUnit tests use five files:

- `JournalApiTests` - CRUD, validation, CSV, presigned URLs and error responses.
- `JournalApiFunctionTests` - Lambda event mapping.
- `RecordsCsvTests` - CSV generation and RFC 4180 escaping.
- `DynamoDbQueryPagerTests` - DynamoDB pagination.
- `ChronologServerStackTests` - CDK stack synthesis.

Manual QA scenarios:

- Create records using both camera and gallery.
- Edit text and verify the Edited date.
- Replace an image and verify the old S3 object is removed.
- Delete with a confirmation popup.
- Toggle Highlight and verify that the colour change persists after sync.
- Enable airplane mode, create records, reconnect and verify sync.
- Stop the app during sync and check that local data remains valid.
- Export CSV and inspect file contents.
- Install the app on a fresh device or clear local app data, then verify remote records restore.

### 5.2 Performance Expectations

- A record with an image normally syncs in 1-3 seconds, including presigned URL request, S3 upload and metadata save.
- Lambda cold start is expected to take approximately 1.5-2 seconds with .NET 8 and 512 MB memory. Warm requests are typically under 100 ms.
- CSV export should complete in under one second for typical personal-journal usage with hundreds of records.

## 6. Change Log

### 6.1 Development Process

Development was incremental, starting from the domain model and working outward:

1. Domain model: `JournalRecord`, validation and sync state.
2. Local persistence: crash-safe JSON repository and image storage.
3. AWS backend: CDK stack, Lambda CRUD API, presigned uploads and CSV export, with server tests using in-memory test doubles.
4. UI and sync: two uGUI screens, coroutine sync service, network monitoring, status bar and loading animation.
5. Iteration: editing and deletion, per-device isolation, highlighting, CSV export and Edited-date display.

### 6.2 Challenges & Mitigation Log

- **DynamoDB listing initially used a single Scan:** it could stop at 1 MB of results. It was replaced with a scoped Query and `DynamoDbQueryPager`, which follows `LastEvaluatedKey`.
- **Fresh install showed Failed with zero records:** remote metadata reload failures incorrectly changed the final sync state. Metadata reload no longer affects the pending-upload failure flag.
- **Offline state was stuck on Syncing:** requests could begin before connectivity was checked. Reachability checks were added before sync and connection errors now transition status to Offline.
- **Delete order initially removed S3 first:** this could leave a DynamoDB record pointing to a missing image. The order was changed to DynamoDB first.
- **Empty state appeared during sync:** the list now waits until sync completes before showing “No records yet”.
- **Android keyboard covered content:** `JournalKeyboardController` uses JNI and `getWindowVisibleDisplayFrame` to keep text input accessible.
- **Offline status was overwritten after sync:** a guard prevents a final sync-state calculation from replacing Offline.

### 6.3 Verification & Validation Results

- All 17 client-side EditMode test files pass.
- All five server-side xUnit test files pass.
- CDK synthesis test confirms that the generated CloudFormation template contains the expected resources.
- Manual testing on physical Android devices covered camera and gallery records, editing, deletion, sync transitions, offline-to-online recovery, CSV export and highlighted records.

### 6.4 Unresolved Anomalies

- The API has no authenticated user identity. This is acceptable only for the test prototype.
- Conflict resolution is last-write-wins. It is unlikely with per-device data, but still possible.
- Failed delete or update operations can leave orphaned S3 files. There is no cleanup job yet.
- Images are uploaded at original resolution. Compression would reduce transfer time and storage.

## 7. Future Improvements

- Add authenticated users with Cognito or another identity provider.
- Upgrade the Lambda runtime from .NET 8 before AWS support ends in November 2026.
- Compress and resize images before upload.
- Support multiple photos per record.
- Add search and filters by text, date and highlighted state.
- Extend database-driven UI behaviour with themes, list styles or a message of the day.
- Add an S3 cleanup job for orphaned files.
- Add pagination to the client list for very large record sets.
- Add PDF export with embedded images.
