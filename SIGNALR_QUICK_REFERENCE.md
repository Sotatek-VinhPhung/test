# SignalR Permission Notifications - Quick Reference

## 🚀 Server API

### Endpoint Configuration
```csharp
// Program.cs - Already configured
app.MapHub<PermissionNotificationHub>("/hubs/permissions");
```

### Sending Notifications

```csharp
// Inject IPermissionNotificationService in your service/controller
private readonly IPermissionNotificationService _notificationService;

// Send role assigned notification
await _notificationService.NotifyRoleAssignedAsync(
    userId: userId,
    roleId: roleId,
    roleName: "Admin",
    cancellationToken: cancellationToken
);

// Send role revoked notification
await _notificationService.NotifyRoleRevokedAsync(
    userId: userId,
    roleId: roleId,
    roleName: "Admin",
    cancellationToken: cancellationToken
);

// Send permission update (broadcasts to all)
await _notificationService.NotifyPermissionsUpdatedAsync(
    roleId: roleId,
    roleName: "Admin",
    subsystemCode: "Reports",
    permissions: flags,
    permissionNames: ["View", "Edit", "Delete"],
    cancellationToken: cancellationToken
);

// Send user permission override
await _notificationService.NotifyUserPermissionOverrideAsync(
    userId: userId,
    subsystemCode: "Analytics",
    permissions: flags,
    isRemoved: false,
    permissionNames: ["View"],
    cancellationToken: cancellationToken
);

// Remove user permission override
await _notificationService.NotifyUserPermissionOverrideRemovedAsync(
    userId: userId,
    subsystemCode: "Analytics",
    cancellationToken: cancellationToken
);
```

## 💻 Client-Side (JavaScript)

### Setup
```javascript
// 1. Import the helper
<script src="./SIGNALR_PERMISSION_NOTIFICATION_CLIENT.js"></script>

// 2. Create instance
const handler = new PermissionNotificationHandler("https://api.example.com");

// 3. Connect with JWT token
const token = localStorage.getItem("authToken");
await handler.connect(token);

// 4. Listen for changes
window.addEventListener("permissionChanged", (e) => {
    const { type, notification } = e.detail;
    console.log(`Permission change: ${type}`, notification);
});

// 5. Disconnect on logout
await handler.disconnect();
```

### Event Handling
```javascript
window.addEventListener("permissionChanged", (e) => {
    const { type, notification } = e.detail;
    
    switch (type) {
        case "roleAssigned":
            console.log("✅ Role assigned:", notification.roleName);
            location.reload(); // Reload to get new permissions
            break;
            
        case "roleRevoked":
            console.log("⚠️ Role revoked:", notification.roleName);
            // Check if user lost access
            if (!handler.hasPermission("MyModule", "View")) {
                location.href = "/no-access";
            }
            break;
            
        case "permissionsUpdated":
            console.log("🔄 Permissions updated:", notification.subsystemCode);
            // Refresh affected permissions
            break;
            
        case "permissionOverride":
            console.log("🔐 Override:", notification.subsystemCode);
            // Update specific subsystem access
            break;
    }
});
```

### Check Permissions
```javascript
// Check if user has permission (from cached data)
const hasAccess = handler.hasPermission("Reports", "Edit");

if (hasAccess) {
    // Show edit button
} else {
    // Hide or disable
}
```

## 📊 Notification Objects

### RoleAssignedNotificationDto
```javascript
{
    userId: "guid",
    roleId: "guid",
    roleName: "Admin",
    changeType: "RoleAssigned",
    changedAt: "2024-01-15T10:30:00Z",
    details: "Role 'Admin' has been assigned to your account"
}
```

### RoleRevokedNotificationDto
```javascript
{
    userId: "guid",
    roleId: "guid",
    roleName: "Admin",
    changeType: "RoleRevoked",
    changedAt: "2024-01-15T10:30:00Z",
    details: "Role 'Admin' has been revoked from your account"
}
```

### PermissionsUpdatedNotificationDto
```javascript
{
    roleId: "guid",
    roleName: "Admin",
    subsystemCode: "Reports",
    permissions: 31,  // bitwise flags
    permissionNames: ["View", "Create", "Edit", "Delete", "Export"],
    changeType: "PermissionsUpdated",
    changedAt: "2024-01-15T10:30:00Z",
    details: "Permissions for role 'Admin' in 'Reports' have been updated"
}
```

### UserPermissionOverrideNotificationDto
```javascript
{
    userId: "guid",
    subsystemCode: "Analytics",
    permissions: 5,  // bitwise flags
    permissionNames: ["View", "Edit"],
    isRemoved: false,
    changeType: "PermissionOverrideApplied",
    changedAt: "2024-01-15T10:30:00Z",
    details: "Permission override for 'Analytics' has been applied"
}
```

## 🔍 localStorage Keys

After connecting and receiving notifications:

```javascript
// User's roles
JSON.parse(localStorage.getItem("userRoles"))
// ["Admin", "Editor"]

// Subsystem permissions (bitwise flags)
JSON.parse(localStorage.getItem("subsystemPermissions"))
// {"Reports": 31, "Analytics": 7, "Users": 15}

// Permission names for each subsystem
JSON.parse(localStorage.getItem("permissionNames"))
// {"Reports": ["View", "Create", "Edit", "Delete", "Export"], ...}

// User-specific permission overrides
JSON.parse(localStorage.getItem("permissionOverrides"))
// {"Analytics": {permissions: 5, permissionNames: ["View", "Edit"]}}
```

## 🎯 Hub Methods (Client → Server)

```javascript
// Subscribe to notifications for a user
await connection.invoke("SubscribeToPermissions", userId);

// Unsubscribe from notifications
await connection.invoke("UnsubscribeFromPermissions", userId);
```

## 📡 Hub Events (Server → Client)

```javascript
// Role assigned
connection.on("RoleAssigned", (notification) => {
    // notification: RoleAssignedNotificationDto
});

// Role revoked
connection.on("RoleRevoked", (notification) => {
    // notification: RoleRevokedNotificationDto
});

// Permissions updated
connection.on("PermissionsUpdated", (notification) => {
    // notification: PermissionsUpdatedNotificationDto
});

// User permission override
connection.on("UserPermissionOverride", (notification) => {
    // notification: UserPermissionOverrideNotificationDto
});
```

## 🔧 Configuration

### Server Configuration (Program.cs)
```csharp
// Already configured
builder.Services.AddSignalR();
app.MapHub<PermissionNotificationHub>("/hubs/permissions");
```

### Client Configuration
```javascript
const handler = new PermissionNotificationHandler(apiUrl);
// Auto-reconnect settings (in code):
// - Initial: 0ms
// - 2nd: 0ms
// - 3rd: 1000ms
// - 4th: 3000ms
// - 5th: 5000ms
// - 6th: 10000ms
```

## 🧪 Testing

```bash
# Terminal 1: Start server
dotnet run --project src/CleanArchitecture.Api

# Terminal 2: Test with curl/Postman
# POST /api/permissions/users/{userId}/roles
# Should see client receive notification
```

```javascript
// Browser Console: Verify connection
handler.connection.state
// Should be "Connected"

// Check if subscribed
localStorage.getItem("userRoles")
// Should have role data

// Manually trigger notification (for testing)
// Admin calls API to assign role
// Check browser console for event
```

## 🚨 Common Issues

| Issue | Fix |
|-------|-----|
| "Cannot read property 'state' of null" | Call `await handler.connect(token)` first |
| No notifications received | Call `SubscribeToPermissions()` in OnConnectedAsync |
| "CORS" error | Configure CORS for SignalR in Program.cs |
| Connection drops | Check network/firewall for WebSocket |
| stale permissions | Refresh page or clear localStorage |

## 📚 Files Reference

| File | Purpose |
|------|---------|
| `PermissionNotificationHub.cs` | SignalR Hub for broadcasting |
| `PermissionNotificationService.cs` | Sends notifications to hub |
| `SIGNALR_PERMISSION_NOTIFICATION_CLIENT.js` | Client-side listener |
| `SIGNALR_PERMISSION_NOTIFICATIONS_GUIDE.md` | Full documentation |
| `SIGNALR_IMPLEMENTATION_COMPLETE.md` | Implementation summary |

---

**Quick Links**:
- 📖 [Full Guide](./SIGNALR_PERMISSION_NOTIFICATIONS_GUIDE.md)
- ✅ [Implementation Summary](./SIGNALR_IMPLEMENTATION_COMPLETE.md)
- 💻 [Client Script](./SIGNALR_PERMISSION_NOTIFICATION_CLIENT.js)
