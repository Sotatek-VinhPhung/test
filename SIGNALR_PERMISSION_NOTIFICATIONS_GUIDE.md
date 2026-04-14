# SignalR Real-Time Permission Notifications

## 📡 Overview

This implementation adds **real-time permission notifications** using **SignalR**. When an admin changes user roles or permissions, all connected clients receive instant notifications to update their local permission cache.

## ✨ Features

- ✅ Real-time role assignment notifications
- ✅ Real-time role revocation notifications
- ✅ Real-time permission update broadcasts
- ✅ User-specific permission override notifications
- ✅ Automatic client cache invalidation
- ✅ Connection auto-reconnect with exponential backoff
- ✅ JWT-based authentication for SignalR
- ✅ Group-based message routing (per user)

## 🏗️ Architecture

```
Admin → Permission API (POST/PUT/DELETE)
  ↓
RoleManagementService (updates DB)
  ↓
IPermissionNotificationService.NotifyXxx()
  ↓
PermissionNotificationHub (broadcasts via SignalR)
  ↓
Client (receives notification → updates localStorage)
```

## 📝 Implementation Details

### Server-Side Components

#### 1. **PermissionNotificationHub** (`src/CleanArchitecture.Api/Hubs/PermissionNotificationHub.cs`)
- SignalR hub for broadcasting permission changes
- Groups users by `user-{userId}` for targeted notifications
- `SubscribeToPermissions(userId)` - Client subscribes
- `UnsubscribeFromPermissions(userId)` - Client unsubscribes

#### 2. **IPermissionNotificationService** (`src/CleanArchitecture.Application/Notifications/Interfaces/IPermissionNotificationService.cs`)
- Interface for notification operations
- Methods:
  - `NotifyRoleAssignedAsync()`
  - `NotifyRoleRevokedAsync()`
  - `NotifyPermissionsUpdatedAsync()`
  - `NotifyUserPermissionOverrideAsync()`
  - `NotifyUserPermissionOverrideRemovedAsync()`

#### 3. **PermissionNotificationService** (`src/CleanArchitecture.Infrastructure/Notifications/PermissionNotificationService.cs`)
- Implements `IPermissionNotificationService`
- Uses `IHubContext<PermissionNotificationHub>` to send messages
- Error handling with logging

#### 4. **DTOs** (`src/CleanArchitecture.Application/Notifications/DTOs/PermissionChangeNotificationDto.cs`)
- `PermissionChangeNotificationDto` - Base class
- `RoleAssignedNotificationDto` - Role assignment details
- `RoleRevokedNotificationDto` - Role revocation details
- `PermissionsUpdatedNotificationDto` - Permission changes
- `UserPermissionOverrideNotificationDto` - User override changes

#### 5. **RoleManagementService** (updated)
- Now injects `IPermissionNotificationService`
- Sends notifications after each operation:
  ```csharp
  await _notificationService.NotifyRoleAssignedAsync(userId, roleId, role.Code, cancellationToken);
  ```

#### 6. **Program.cs** (updated)
- Registered SignalR: `builder.Services.AddSignalR();`
- Mapped hub: `app.MapHub<PermissionNotificationHub>("/hubs/permissions");`

### Client-Side Implementation

#### JavaScript Helper (`SIGNALR_PERMISSION_NOTIFICATION_CLIENT.js`)

**Class**: `PermissionNotificationHandler`

```javascript
// 1. Initialize
const handler = new PermissionNotificationHandler("https://api.example.com");

// 2. Connect with JWT token
await handler.connect(jwtToken);

// 3. Listen for events
window.addEventListener("permissionChanged", (e) => {
    const { type, notification } = e.detail;
    console.log("Permission changed:", type, notification);
    // Update UI, clear cache, reload, etc.
});

// 4. Check permissions from cache
const hasAccess = handler.hasPermission("Reports", "View");

// 5. Disconnect on logout
await handler.disconnect();
```

**Features**:
- Automatic JWT token decoding
- Auto-reconnect with exponential backoff
- localStorage cache management
- Custom event emission
- User notifications (Toastr/Bootstrap/console)

## 🚀 Usage Guide

### Server: Send Notification

```csharp
// When assigning role
await _notificationService.NotifyRoleAssignedAsync(
    userId: userId,
    roleId: roleId,
    roleName: role.Code,
    cancellationToken: cancellationToken
);

// When updating role permissions
await _notificationService.NotifyPermissionsUpdatedAsync(
    roleId: roleId,
    roleName: role.Code,
    subsystemCode: subsystem.Code,
    permissions: flags,
    permissionNames: permissionNames,
    cancellationToken: cancellationToken
);

// When overriding user permissions
await _notificationService.NotifyUserPermissionOverrideAsync(
    userId: userId,
    subsystemCode: subsystem.Code,
    permissions: flags,
    isRemoved: false,
    permissionNames: permissionNames,
    cancellationToken: cancellationToken
);
```

### Client: Receive Notifications

**HTML Setup**:
```html
<!-- Include SignalR client library -->
<script src="https://cdn.jsdelivr.net/npm/@microsoft/signalr@latest/signalr.min.js"></script>

<!-- Include permission handler -->
<script src="./SIGNALR_PERMISSION_NOTIFICATION_CLIENT.js"></script>
```

**JavaScript Setup**:
```javascript
// After user logs in and gets JWT token
const handler = new PermissionNotificationHandler("https://api.example.com");
await handler.connect(jwtToken);

// Listen for changes
window.addEventListener("permissionChanged", (e) => {
    const { type, notification } = e.detail;
    
    switch (type) {
        case "roleAssigned":
            console.log("✅ Role assigned:", notification.roleName);
            // Reload page or update UI
            location.reload();
            break;
            
        case "roleRevoked":
            console.log("⚠️ Role revoked:", notification.roleName);
            // Check if user lost access to current page
            if (!handler.hasPermission("CurrentModule", "View")) {
                location.href = "/no-access";
            }
            break;
            
        case "permissionsUpdated":
            console.log("🔄 Permissions updated for:", notification.subsystemCode);
            // Refresh permissions for affected subsystem
            break;
            
        case "permissionOverride":
            console.log("🔐 Permission override:", notification.subsystemCode);
            // Update user permissions cache
            break;
    }
});

// On logout
window.addEventListener("beforeunload", async () => {
    await handler.disconnect();
});
```

## 📊 Notification Flow Examples

### Example 1: Admin Assigns Role

```
Admin POST /api/permissions/users/{userId}/roles
  ↓
RoleManagementService.AssignRoleToUserAsync()
  ↓
DB: INSERT INTO user_roles
  ↓
InvalidateUserContextAsync() - Clear server cache
  ↓
NotifyRoleAssignedAsync() - Send SignalR message
  ↓
Hub sends to group "user-{userId}"
  ↓
Client receives "RoleAssigned" event
  ↓
Client updates localStorage["userRoles"]
  ↓
Client dispatches "permissionChanged" event
  ↓
React component/page updates or reloads
```

### Example 2: Admin Updates Role Permissions

```
Admin PUT /api/permissions/roles/{roleId}/subsystems/{subsystemId}/permissions
  ↓
RoleManagementService.UpdateRolePermissionsAsync()
  ↓
DB: UPDATE role_subsystem_permissions
  ↓
InvalidateRoleUsersContextAsync() - Clear all affected users' cache
  ↓
NotifyPermissionsUpdatedAsync() - Broadcast to ALL clients
  ↓
Hub sends to "All" clients
  ↓
All connected clients receive "PermissionsUpdated"
  ↓
Each client updates localStorage["subsystemPermissions"]
  ↓
Clients with affected roles reload or update UI
```

## 🔒 Security Considerations

1. **Authentication**: SignalR connection requires valid JWT token
2. **Authorization**: Only users in the hub can receive their own notifications
3. **Group Isolation**: Messages sent to `user-{userId}` groups only reach that user
4. **Token Validation**: `PermissionNotificationHub.OnConnectedAsync()` verifies user context

## ⚙️ Configuration

### appsettings.json

```json
{
  "SignalR": {
    "MaximumReceiveMessageSize": 32768,
    "KeepAliveInterval": "15s"
  }
}
```

### Program.cs

```csharp
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 32 * 1024; // 32KB
});
```

## 🧪 Testing

### Test Scenario 1: Role Assignment

1. Admin logs in as administrator
2. Assign new role to user via API
3. User should see notification in browser
4. User's localStorage should update
5. Verify user can now access new resources

### Test Scenario 2: Permission Override

1. Admin logs in
2. Override user permissions via API
3. User receives notification
4. User's cache updates
5. User's access changes immediately

### Test Scenario 3: Multi-Tab Sync

1. Open same app in 2 browser tabs
2. Tab 1: User logs in, sees their roles
3. Tab 2: Admin assigns new role
4. Tab 1: Should receive notification
5. Tab 1: localStorage updates (shared across tabs)

## 🛠️ Troubleshooting

| Issue | Cause | Solution |
|-------|-------|----------|
| Connection fails | JWT token invalid | Verify token in browser DevTools |
| No notifications received | Not subscribed to group | Call `SubscribeToPermissions()` after connect |
| Repeated reconnections | Network unstable | Check firewall/proxy for WebSocket support |
| localStorage empty | Notification not handled | Check browser console for errors |
| Old permissions cached | Cache not invalidated | Refresh page or clear localStorage |

## 📚 Reference

### SignalR Hub Methods (Server → Client)

```csharp
// Called when role is assigned
await Clients.Group($"user-{userId}").SendAsync("RoleAssigned", notification);

// Called when role is revoked
await Clients.Group($"user-{userId}").SendAsync("RoleRevoked", notification);

// Called when role permissions change (to ALL clients)
await Clients.All.SendAsync("PermissionsUpdated", notification);

// Called when user permissions are overridden
await Clients.Group($"user-{userId}").SendAsync("UserPermissionOverride", notification);
```

### Client Event Listeners

```javascript
// Invoked on client side
this.connection.on("RoleAssigned", (notification) => { });
this.connection.on("RoleRevoked", (notification) => { });
this.connection.on("PermissionsUpdated", (notification) => { });
this.connection.on("UserPermissionOverride", (notification) => { });
```

## ✅ Next Steps

1. ✅ Server implementation complete
2. ✅ Client JavaScript helper provided
3. ⏳ Test with React/Vue component integration
4. ⏳ Monitor SignalR connection metrics
5. ⏳ Add admin dashboard for monitoring active connections
6. ⏳ Implement reconnection retry strategies

## 📞 Support

For issues or questions:
1. Check browser console for errors
2. Verify JWT token validity
3. Check network tab for WebSocket connection
4. Review server logs for SignalR errors
5. Ensure firewall allows WebSocket (port 443/80)
