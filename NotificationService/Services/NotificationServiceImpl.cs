using Grpc.Core;
using NotificationService;

namespace NotificationService.Services
{
    /// <summary>
    /// Implementation of the Notification Service that handles sending notifications to users.
    /// Part of the SAGA pattern for distributed transaction management.
    /// </summary>
    public class NotificationServiceImpl : NotificationService.NotificationServiceBase
    {
        private readonly ILogger<NotificationServiceImpl> _logger;
        private readonly List<Notification> _notifications = new();

        public NotificationServiceImpl(ILogger<NotificationServiceImpl> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Sends a notification to a specified recipient.
        /// </summary>
        public override async Task<NotificationResponse> SendNotification(NotificationRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation($"Sending notification to {request.RecipientId} of type {request.Type}");

                var notification = new Notification
                {
                    NotificationId = Guid.NewGuid().ToString(),
                    RecipientId = request.RecipientId,
                    Type = request.Type,
                    Message = request.Message,
                    Metadata = request.Metadata.ToDictionary(kv => kv.Key, kv => kv.Value),
                    Timestamp = DateTime.UtcNow
                };

                _notifications.Add(notification);

                // In a real implementation, this would send the notification through appropriate channels
                // (email, SMS, push notification, etc.)
                _logger.LogInformation($"Notification {notification.NotificationId} sent successfully");

                return new NotificationResponse
                {
                    Success = true,
                    Message = "Notification sent successfully",
                    NotificationId = notification.NotificationId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification");
                return new NotificationResponse
                {
                    Success = false,
                    Message = $"Error sending notification: {ex.Message}"
                };
            }
        }

        public override async Task<NotificationResponse> SendFailureNotification(FailureNotificationRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation($"Sending failure notification to {request.RecipientId} for service {request.ServiceName}");

                var notification = new Notification
                {
                    NotificationId = Guid.NewGuid().ToString(),
                    RecipientId = request.RecipientId,
                    Type = NotificationType.OrderCancelled,
                    Message = $"Transaction failed: {request.ErrorMessage}",
                    Metadata = new Dictionary<string, string>
                    {
                        { "TransactionId", request.TransactionId },
                        { "ServiceName", request.ServiceName }
                    },
                    Timestamp = DateTime.UtcNow
                };

                _notifications.Add(notification);

                // In a real implementation, this would send the failure notification through appropriate channels
                _logger.LogInformation($"Failure notification {notification.NotificationId} sent successfully");

                return new NotificationResponse
                {
                    Success = true,
                    Message = "Failure notification sent successfully",
                    NotificationId = notification.NotificationId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending failure notification");
                return new NotificationResponse
                {
                    Success = false,
                    Message = $"Error sending failure notification: {ex.Message}"
                };
            }
        }
    }

    public class Notification
    {
        public string NotificationId { get; set; } = string.Empty;
        public string RecipientId { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, string> Metadata { get; set; } = new();
        public DateTime Timestamp { get; set; }
    }
} 