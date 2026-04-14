using CleanArchitecture.Application.Auth.Events;
using CleanArchitecture.Domain.Interfaces;
using CleanArchitecture.Domain.Interfaces.Messaging;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.Users.Services;

/// <summary>
/// Handles UserLoginEvent messages from Kafka.
/// Logs user login activity and can be extended for analytics, notifications, etc.
/// </summary>
public class UserLoginEventHandler : IMessageHandler<UserLoginEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserLoginEventHandler> _logger;

    public string Topic => "user-login-events";

    public UserLoginEventHandler(IUnitOfWork unitOfWork, ILogger<UserLoginEventHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(UserLoginEvent message, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Processing login event for user {UserId} ({Email}) with role {Role} at {LoginAt}",
                message.UserId, message.Email, message.Role, message.LoginAt);

            // Example: You can extend this to:
            // 1. Update user's last login timestamp
            var user = await _unitOfWork.Users.GetByIdAsync(message.UserId, cancellationToken);
            if (user is not null)
            {
                // Update last login timestamp (if such field exists in User entity)
                // user.LastLoginAt = message.LoginAt;
                // _unitOfWork.Users.Update(user);
                // await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully handled login event for user {UserId}", message.UserId);
            }
            else
            {
                _logger.LogWarning("User {UserId} not found when processing login event", message.UserId);
            }

            // 2. Log to analytics service
            // 3. Send notification/email
            // 4. Update user statistics
            // 5. Trigger other business logic
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing login event for user {UserId}", message.UserId);
            throw; // Re-throw to trigger DLQ handling
        }
    }
}
