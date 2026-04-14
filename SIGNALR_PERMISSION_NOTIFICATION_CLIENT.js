/**
 * SignalR Permission Notification Handler
 * Client-side listener for real-time permission change notifications
 * 
 * Usage:
 * const notificationHandler = new PermissionNotificationHandler("https://your-api.com");
 * await notificationHandler.connect(jwtToken);
 * 
 * // Listener will automatically update localStorage when permissions change
 */

class PermissionNotificationHandler {
    constructor(apiUrl) {
        this.apiUrl = apiUrl;
        this.connection = null;
        this.userId = null;
    }

    /**
     * Initialize connection to SignalR hub
     */
    async connect(jwtToken) {
        // Decode JWT to get userId
        this.userId = this.decodeJwt(jwtToken).sub;

        // Import SignalR library (ensure it's loaded in your HTML)
        // <script src="https://cdn.jsdelivr.net/npm/@microsoft/signalr@latest/signalr.min.js"></script>

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(`${this.apiUrl}/hubs/permissions`, {
                accessTokenFactory: () => jwtToken,
                skipNegotiation: true,
                transport: signalR.HttpTransportType.WebSockets
            })
            .withAutomaticReconnect([0, 0, 1000, 3000, 5000, 10000])
            .withHubProtocol(new signalR.JsonHubProtocol())
            .build();

        // Setup event listeners
        this.setupListeners();

        // Start connection
        try {
            await this.connection.start();
            console.log("✅ Connected to Permission Notification Hub");

            // Subscribe to this user's permission updates
            await this.connection.invoke("SubscribeToPermissions", this.userId);
            console.log("✅ Subscribed to permission notifications");
        } catch (error) {
            console.error("❌ Connection failed:", error);
            throw error;
        }
    }

    /**
     * Setup SignalR event listeners
     */
    setupListeners() {
        // Listen for role assignment
        this.connection.on("RoleAssigned", (notification) => {
            console.log("🔔 Role Assigned:", notification);
            this.handleRoleAssigned(notification);
        });

        // Listen for role revocation
        this.connection.on("RoleRevoked", (notification) => {
            console.log("🔔 Role Revoked:", notification);
            this.handleRoleRevoked(notification);
        });

        // Listen for permission updates
        this.connection.on("PermissionsUpdated", (notification) => {
            console.log("🔔 Permissions Updated:", notification);
            this.handlePermissionsUpdated(notification);
        });

        // Listen for user permission overrides
        this.connection.on("UserPermissionOverride", (notification) => {
            console.log("🔔 Permission Override:", notification);
            this.handlePermissionOverride(notification);
        });

        // Connection state changes
        this.connection.onclose(async () => {
            console.log("⚠️ Disconnected from hub. Attempting to reconnect...");
        });

        this.connection.onreconnected(() => {
            console.log("✅ Reconnected to hub");
        });
    }

    /**
     * Handle role assignment notification
     */
    handleRoleAssigned(notification) {
        // Show user notification
        this.showNotification("info", notification.details);

        // Store in localStorage (optional)
        const roles = JSON.parse(localStorage.getItem("userRoles") || "[]");
        if (!roles.includes(notification.roleName)) {
            roles.push(notification.roleName);
            localStorage.setItem("userRoles", JSON.stringify(roles));
        }

        // Emit custom event for app to react
        this.dispatchPermissionChangeEvent("roleAssigned", notification);

        // Reload permissions if needed
        // window.location.reload(); // Uncomment if automatic reload is needed
    }

    /**
     * Handle role revocation notification
     */
    handleRoleRevoked(notification) {
        // Show user notification
        this.showNotification("warning", notification.details);

        // Update localStorage
        const roles = JSON.parse(localStorage.getItem("userRoles") || "[]");
        const index = roles.indexOf(notification.roleName);
        if (index > -1) {
            roles.splice(index, 1);
            localStorage.setItem("userRoles", JSON.stringify(roles));
        }

        // Emit custom event
        this.dispatchPermissionChangeEvent("roleRevoked", notification);

        // Reload if user lost critical permissions
        // window.location.reload();
    }

    /**
     * Handle permission updates notification
     * Note: This affects all users, so we should refresh permissions
     */
    handlePermissionsUpdated(notification) {
        // Show notification
        this.showNotification("info", notification.details);

        // Store updated permissions
        const permissions = JSON.parse(localStorage.getItem("subsystemPermissions") || "{}");
        permissions[notification.subsystemCode] = notification.permissions;
        localStorage.setItem("subsystemPermissions", JSON.stringify(permissions));

        // Store permission names for easy checking
        if (notification.permissionNames) {
            const permNames = JSON.parse(localStorage.getItem("permissionNames") || "{}");
            permNames[notification.subsystemCode] = notification.permissionNames;
            localStorage.setItem("permissionNames", JSON.stringify(permNames));
        }

        // Emit event
        this.dispatchPermissionChangeEvent("permissionsUpdated", notification);

        // Optionally reload to reflect changes
        // window.location.reload();
    }

    /**
     * Handle user-specific permission override
     */
    handlePermissionOverride(notification) {
        const action = notification.isRemoved ? "removed" : "applied";
        this.showNotification("info", notification.details);

        // Store override
        const overrides = JSON.parse(localStorage.getItem("permissionOverrides") || "{}");
        
        if (notification.isRemoved) {
            delete overrides[notification.subsystemCode];
        } else {
            overrides[notification.subsystemCode] = {
                permissions: notification.permissions,
                permissionNames: notification.permissionNames
            };
        }

        localStorage.setItem("permissionOverrides", JSON.stringify(overrides));

        // Emit event
        this.dispatchPermissionChangeEvent("permissionOverride", notification);
    }

    /**
     * Display user-friendly notification
     */
    showNotification(type, message) {
        // If using a notification library like Toastr
        if (window.toastr) {
            window.toastr[type](message);
        }
        // If using Bootstrap alerts
        else if (window.bootstrap) {
            const alertDiv = document.createElement("div");
            alertDiv.className = `alert alert-${type === "info" ? "info" : type === "warning" ? "warning" : "danger"} alert-dismissible fade show`;
            alertDiv.innerHTML = `${message}<button type="button" class="btn-close" data-bs-dismiss="alert"></button>`;
            document.body.prepend(alertDiv);
            setTimeout(() => alertDiv.remove(), 5000);
        }
        // Fallback: console log
        else {
            console.log(`[${type.toUpperCase()}] ${message}`);
        }
    }

    /**
     * Emit custom event for permission changes
     */
    dispatchPermissionChangeEvent(eventType, notification) {
        const event = new CustomEvent("permissionChanged", {
            detail: {
                type: eventType,
                notification: notification,
                timestamp: new Date().toISOString()
            }
        });
        window.dispatchEvent(event);
    }

    /**
     * Disconnect from hub
     */
    async disconnect() {
        if (this.connection) {
            try {
                await this.connection.invoke("UnsubscribeFromPermissions", this.userId);
            } catch (e) {
                console.warn("Could not unsubscribe:", e);
            }
            await this.connection.stop();
            console.log("✅ Disconnected from hub");
        }
    }

    /**
     * Helper: Decode JWT token
     */
    decodeJwt(token) {
        const base64Url = token.split(".")[1];
        const base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/");
        const jsonPayload = decodeURIComponent(
            atob(base64)
                .split("")
                .map((c) => "%" + ("00" + c.charCodeAt(0).toString(16)).slice(-2))
                .join("")
        );
        return JSON.parse(jsonPayload);
    }

    /**
     * Helper: Check if user has permission (from cached permissions)
     */
    hasPermission(subsystemCode, permissionName) {
        const overrides = JSON.parse(localStorage.getItem("permissionOverrides") || "{}");
        
        // Check override first
        if (overrides[subsystemCode]?.permissionNames) {
            return overrides[subsystemCode].permissionNames.includes(permissionName);
        }

        // Check role permissions
        const permNames = JSON.parse(localStorage.getItem("permissionNames") || "{}");
        if (permNames[subsystemCode]) {
            return permNames[subsystemCode].includes(permissionName);
        }

        return false;
    }
}

// Export for use in modules
if (typeof module !== "undefined" && module.exports) {
    module.exports = PermissionNotificationHandler;
}
