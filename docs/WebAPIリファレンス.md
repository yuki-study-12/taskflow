# TaskFlow WebAPI リファレンス

## 1. 概要

| 項目 | 内容 |
|---|---|
| Base URL（開発環境） | `https://localhost:7192`（または `http://localhost:5075`） |
| Content-Type | `application/json` |
| 認証方式 | JWT Bearer トークン（`Authorization: Bearer <token>`） |
| OpenAPI/Swagger | 開発環境のみ `/openapi` で定義を取得可能。Swagger UI等から実際に試すこともできる |
| リアルタイム通信 | SignalR — `/hubs/board`、`/hubs/notification`（詳細は機能仕様書を参照） |

トークンの有効期限は既定60分（`JwtSettings:ExpiryMinutes`）。期限切れ後は `401 Unauthorized` が返るため、再度ログインしてトークンを取得する。

## 2. 認証フロー

1. `POST /api/auth/register`（初回のみ）または `POST /api/auth/login` を呼び、レスポンスの `token` を取得する。
2. 以降のすべてのリクエストに `Authorization: Bearer <token>` ヘッダーを付与する。

```bash
# ログインしてトークンを取得
curl -k -X POST https://localhost:7192/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"password123"}'

# レスポンス例
# {
#   "userId": "8f14e...",
#   "email": "user@example.com",
#   "displayName": "yuki",
#   "token": "eyJhbGciOi..."
# }

# 取得したトークンを使ってAPIを呼ぶ
curl -k https://localhost:7192/api/projects \
  -H "Authorization: Bearer eyJhbGciOi..."
```

## 3. エラーレスポンス形式

未処理例外の種類に応じて、`GlobalExceptionHandler` が以下のHTTPステータスとボディを返す。

| 例外 | ステータス | ボディ形式 |
|---|---|---|
| バリデーションエラー（FluentValidation） | 400 Bad Request | `ValidationProblemDetails`（`errors` にフィールド名ごとのエラー配列） |
| `NotFoundException` | 404 Not Found | `ProblemDetails`（`title: "Not Found"`） |
| `ForbiddenAccessException` | 403 Forbidden | `ProblemDetails`（`title: "Forbidden"`） |
| その他の未処理例外 | 500 Internal Server Error | `ProblemDetails`（`title: "Internal Server Error"`）。裏でAI例外解析パイプラインが起動する（機能仕様書10章参照） |

`ProblemDetails` の例：
```json
{
  "status": 404,
  "title": "Not Found",
  "detail": "Task (Id: 8f14e...) was not found.",
  "type": "https://httpstatuses.io/404"
}
```

> 例外: `POST /api/auth/register` と `POST /api/auth/login` のバリデーション/認証失敗時のみ、上記と異なり `string[]`（エラーメッセージの配列）をそのまま返す（`400 Bad Request`）。

未指定パラメータの型不一致（例: GUIDでないIDをルートに渡す）は ASP.NET Core のルート制約（`{id:guid}`）により自動的に `404 Not Found` になる。

## 4. 共通レスポンススキーマ

以降の章で参照する型定義。

**AuthResponse**
```json
{ "userId": "guid", "email": "string", "displayName": "string", "token": "string" }
```

**UserProfileResponse**
```json
{ "userId": "guid", "email": "string", "displayName": "string", "avatarUrl": "string|null" }
```

**ProjectResponse**
```json
{ "id": "guid", "name": "string", "description": "string", "userRole": "Owner|Admin|Member" }
```

**MemberResponse**
```json
{ "userId": "guid", "role": "Owner|Admin|Member", "displayName": "string", "avatarUrl": "string|null" }
```

**BoardResponse**
```json
{
  "id": "guid", "projectId": "guid", "name": "string",
  "columns": [ /* ColumnResponse[] */ ]
}
```

**ColumnResponse**
```json
{
  "id": "guid", "boardId": "guid", "name": "string", "order": 0,
  "tasks": [ /* TaskResponse[] */ ]
}
```

**TaskResponse**
```json
{
  "id": "guid", "projectId": "guid", "columnId": "guid",
  "title": "string", "description": "string",
  "assigneeId": "guid|null", "order": 0,
  "dueDate": "2026-08-19T00:00:00|null",
  "priority": "Low|Medium|High|Critical",
  "createdAt": "datetime", "updatedAt": "datetime"
}
```

**MyTaskResponse**（`/api/tasks/mine` 専用。担当プロジェクト・列の名前を含む）
```json
{
  "id": "guid", "title": "string", "description": "string",
  "projectId": "guid", "projectName": "string",
  "columnId": "guid", "columnName": "string",
  "createdAt": "datetime", "updatedAt": "datetime"
}
```

**CommentResponse**
```json
{
  "id": "guid", "taskId": "guid", "authorId": "guid",
  "authorDisplayName": "string", "body": "string", "createdAt": "datetime"
}
```

**NotificationResponse**
```json
{
  "id": "guid", "message": "string",
  "type": "TaskAssigned|TaskCommented|MemberInvited",
  "relatedEntityId": "guid", "isRead": false, "createdAt": "datetime"
}
```

## 5. Auth（`/api/auth`）

### `POST /api/auth/register` — 認証: 不要
アカウント登録。

リクエスト:
```json
{ "email": "user@example.com", "password": "password123", "displayName": "yuki" }
```
バリデーション: Email形式必須 / Password 8〜100文字 / DisplayName 1〜50文字

レスポンス: `200 OK` → `AuthResponse` ／ 失敗時 `400 Bad Request` → `string[]`

### `POST /api/auth/login` — 認証: 不要
ログイン。

リクエスト: `{ "email": "string", "password": "string" }`

レスポンス: `200 OK` → `AuthResponse` ／ 失敗時 `400 Bad Request` → `string[]`

### `GET /api/auth/me` — 認証: 必須
ログイン中ユーザーの情報を取得。

レスポンス: `200 OK` → `UserProfileResponse` ／ `401 Unauthorized`

## 6. Projects（`/api/projects`）

| Method | Path | 概要 | 権限 |
|---|---|---|---|
| GET | `/api/projects` | 所属プロジェクト一覧 | メンバー |
| GET | `/api/projects/{id}` | プロジェクト詳細 | メンバー |
| POST | `/api/projects` | プロジェクト作成 | 誰でも（作成者がOwnerになる） |
| PUT | `/api/projects/{id}` | 名称・説明更新 | メンバー |
| DELETE | `/api/projects/{id}` | プロジェクト削除 | Owner |

### `GET /api/projects`
レスポンス: `200 OK` → `ProjectResponse[]`

### `GET /api/projects/{id}`
レスポンス: `200 OK` → `ProjectResponse`

### `POST /api/projects`
```json
{ "name": "新規プロジェクト", "description": "説明文" }
```
バリデーション: Name必須・200文字以内／Description必須・1000文字以内
レスポンス: `201 Created` → `ProjectResponse`（`Location: /api/projects/{id}`）

```bash
curl -k -X POST https://localhost:7192/api/projects \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"name":"新規プロジェクト","description":"説明文"}'
```

### `PUT /api/projects/{id}`
```json
{ "name": "更新後の名前", "description": "更新後の説明" }
```
レスポンス: `200 OK` → `ProjectResponse`

### `DELETE /api/projects/{id}`
レスポンス: `204 No Content`。**Owner以外が呼ぶと403。**

## 7. Members（`/api/projects/{id}/members`）

| Method | Path | 概要 | 権限 |
|---|---|---|---|
| GET | `/api/projects/{id}/members` | メンバー一覧 | メンバー |
| POST | `/api/projects/{id}/members` | メンバー招待 | Owner/Admin |
| PUT | `/api/projects/{id}/members/{uid}` | ロール変更 | Owner/Admin |
| DELETE | `/api/projects/{id}/members/{uid}` | メンバー削除 | Owner/Admin |

### `GET /api/projects/{id}/members`
レスポンス: `200 OK` → `MemberResponse[]`

### `POST /api/projects/{id}/members`
```json
{ "userId": "招待したいユーザーのguid", "role": "Member" }
```
`role` は `"Admin"` または `"Member"` のみ有効（`"Owner"` は不可）。
レスポンス: `201 Created` → `MemberResponse`

```bash
curl -k -X POST https://localhost:7192/api/projects/$PROJECT_ID/members \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"userId":"'"$TARGET_USER_ID"'","role":"Member"}'
```

### `PUT /api/projects/{id}/members/{uid}`
```json
{ "role": "Admin" }
```
レスポンス: `200 OK` → `MemberResponse`

### `DELETE /api/projects/{id}/members/{uid}`
レスポンス: `204 No Content`

## 8. Boards / Columns（`/api/projects`, `/api/boards`, `/api/columns`）

| Method | Path | 概要 |
|---|---|---|
| GET | `/api/projects/{id}/board` | ボード取得（列・タスクを含む） |
| POST | `/api/projects/{id}/boards` | ボード作成 |
| POST | `/api/boards/{id}/columns` | 列の作成 |
| PUT | `/api/columns/{id}` | 列名の変更 |

### `GET /api/projects/{id}/board`
レスポンス: `200 OK` → `BoardResponse`（`columns[].tasks[]` まで含む階層構造。列は `Order` 昇順、タスクも `Order` 昇順）

### `POST /api/projects/{id}/boards`
```json
{ "name": "メインボード" }
```
レスポンス: `201 Created` → `BoardResponse`

### `POST /api/boards/{id}/columns`
```json
{ "name": "レビュー中" }
```
レスポンス: `201 Created` → `ColumnResponse`

### `PUT /api/columns/{id}`
```json
{ "name": "新しい列名" }
```
レスポンス: `200 OK` → `ColumnResponse`

## 9. Tasks（`/api/tasks`, `/api/columns/{id}/tasks`）

| Method | Path | 概要 |
|---|---|---|
| POST | `/api/columns/{id}/tasks` | タスク作成 |
| GET | `/api/tasks/mine` | 自分の担当タスク一覧（全プロジェクト横断） |
| GET | `/api/tasks/{id}` | タスク詳細 |
| PUT | `/api/tasks/{id}` | タスク更新 |
| PUT | `/api/tasks/{id}/move` | タスク移動（ドラッグ&ドロップ相当） |
| DELETE | `/api/tasks/{id}` | タスク削除 |

### `POST /api/columns/{id}/tasks`
```json
{ "title": "実装タスク", "description": "詳細説明", "assigneeId": null }
```
バリデーション: Title必須・500文字以内／Description 2000文字以内
レスポンス: `201 Created` → `TaskResponse`

> 注意: この方法で `assigneeId` を指定して作成しても、`TaskAssigned` 通知は**発火しない**（後述の`PUT /api/tasks/{id}`での変更時のみ発火）。

### `GET /api/tasks/mine`
レスポンス: `200 OK` → `MyTaskResponse[]`

### `GET /api/tasks/{id}`
レスポンス: `200 OK` → `TaskResponse`

### `PUT /api/tasks/{id}`
```json
{
  "title": "更新後タイトル",
  "description": "更新後説明",
  "assigneeId": "guid|null",
  "dueDate": "2026-08-31T00:00:00",
  "priority": "High"
}
```
`priority` は `"Low"` / `"Medium"` / `"High"` / `"Critical"` のいずれか。不正な文字列を渡すと `400 Bad Request`（プレーンテキスト `"Invalid priority: xxx"`）。
`assigneeId` が新しい値に変更された場合、対象ユーザーに `TaskAssigned` 通知が飛ぶ。

レスポンス: `200 OK` → `TaskResponse`

```bash
curl -k -X PUT https://localhost:7192/api/tasks/$TASK_ID \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"title":"更新後タイトル","description":"更新後説明","assigneeId":"'"$USER_ID"'","dueDate":"2026-08-31T00:00:00","priority":"High"}'
```

### `PUT /api/tasks/{id}/move`
```json
{ "targetColumnId": "guid", "newOrder": 0 }
```
レスポンス: `200 OK` → `TaskResponse`。同じボードを見ている他クライアントへ SignalR `TaskMoved` イベントがブロードキャストされる。

### `DELETE /api/tasks/{id}`
レスポンス: `204 No Content`

## 10. Comments（`/api/tasks/{id}/comments`, `/api/comments`）

| Method | Path | 概要 | 権限 |
|---|---|---|---|
| GET | `/api/tasks/{id}/comments` | コメント一覧 | メンバー |
| POST | `/api/tasks/{id}/comments` | コメント投稿 | メンバー |
| DELETE | `/api/comments/{id}` | コメント削除 | 投稿者本人のみ |

### `GET /api/tasks/{id}/comments`
レスポンス: `200 OK` → `CommentResponse[]`

### `POST /api/tasks/{id}/comments`
```json
{ "body": "コメント本文" }
```
バリデーション: 必須・2000文字以内。投稿するとタスクの担当者・作成者（投稿者本人を除く）に `TaskCommented` 通知が飛ぶ。
レスポンス: `201 Created` → `CommentResponse`

### `DELETE /api/comments/{id}`
レスポンス: `204 No Content`。投稿者本人以外が呼ぶと `403 Forbidden`。

## 11. Notifications（`/api/notifications`）

| Method | Path | 概要 |
|---|---|---|
| GET | `/api/notifications` | 自分宛の通知一覧 |
| PUT | `/api/notifications/{id}/read` | 1件既読化 |
| PUT | `/api/notifications/read-all` | 全件既読化 |

### `GET /api/notifications`
レスポンス: `200 OK` → `NotificationResponse[]`

### `PUT /api/notifications/{id}/read`
レスポンス: `200 OK` → `NotificationResponse`。他ユーザー宛の通知を既読化しようとすると `403 Forbidden`。

### `PUT /api/notifications/read-all`
レスポンス: `204 No Content`

## 12. エンドポイント一覧（早見表）

| Method | Path | 認証 | 概要 |
|---|---|:---:|---|
| POST | `/api/auth/register` | - | アカウント登録 |
| POST | `/api/auth/login` | - | ログイン |
| GET | `/api/auth/me` | ✓ | 自分の情報取得 |
| GET | `/api/projects` | ✓ | プロジェクト一覧 |
| GET | `/api/projects/{id}` | ✓ | プロジェクト詳細 |
| POST | `/api/projects` | ✓ | プロジェクト作成 |
| PUT | `/api/projects/{id}` | ✓ | プロジェクト更新 |
| DELETE | `/api/projects/{id}` | ✓ | プロジェクト削除（Owner） |
| GET | `/api/projects/{id}/members` | ✓ | メンバー一覧 |
| POST | `/api/projects/{id}/members` | ✓ | メンバー招待（Owner/Admin） |
| PUT | `/api/projects/{id}/members/{uid}` | ✓ | ロール変更（Owner/Admin） |
| DELETE | `/api/projects/{id}/members/{uid}` | ✓ | メンバー削除（Owner/Admin） |
| GET | `/api/projects/{id}/board` | ✓ | ボード取得 |
| POST | `/api/projects/{id}/boards` | ✓ | ボード作成 |
| POST | `/api/boards/{id}/columns` | ✓ | 列作成 |
| PUT | `/api/columns/{id}` | ✓ | 列名変更 |
| POST | `/api/columns/{id}/tasks` | ✓ | タスク作成 |
| GET | `/api/tasks/mine` | ✓ | 自分の担当タスク一覧 |
| GET | `/api/tasks/{id}` | ✓ | タスク詳細 |
| PUT | `/api/tasks/{id}` | ✓ | タスク更新 |
| PUT | `/api/tasks/{id}/move` | ✓ | タスク移動 |
| DELETE | `/api/tasks/{id}` | ✓ | タスク削除 |
| GET | `/api/tasks/{id}/comments` | ✓ | コメント一覧 |
| POST | `/api/tasks/{id}/comments` | ✓ | コメント投稿 |
| DELETE | `/api/comments/{id}` | ✓ | コメント削除（投稿者本人） |
| GET | `/api/notifications` | ✓ | 通知一覧 |
| PUT | `/api/notifications/{id}/read` | ✓ | 通知を既読化 |
| PUT | `/api/notifications/read-all` | ✓ | 全通知を既読化 |

## 13. SignalR Hub 早見表

| Hub | パス | クライアント→サーバー | サーバー→クライアント |
|---|---|---|---|
| BoardHub | `/hubs/board` | `JoinBoard(boardId)` / `LeaveBoard(boardId)` | `TaskMoved(task)` |
| NotificationHub | `/hubs/notification` | `JoinUserGroup(userId)` / `LeaveUserGroup(userId)` | `UnreadCountUpdated(count)` |

接続にはクエリ文字列またはヘッダーでJWTトークンを渡す必要がある（詳細は `TaskFlow.Web/Services` のSignalR接続実装を参照）。

---

権限モデル・通知トリガーの詳細な仕様は `docs/機能仕様書.md` を、画面操作の説明は `docs/利用説明書.md` を参照してください。
