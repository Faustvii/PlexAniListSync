using System.Threading.Channels;
using PlexAniListSync.Models.Notifications;

namespace PlexAniListSync.Services.Notifications;

internal sealed record QueuedNotification(
    NotificationTarget Target,
    string Title,
    string Message,
    string Tags,
    string Priority,
    string? Click
);

internal sealed class NotificationQueue
{
    private readonly Channel<QueuedNotification> _channel = Channel.CreateBounded<QueuedNotification>(
        new BoundedChannelOptions(64)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        }
    );

    public void Enqueue(QueuedNotification notification) => _channel.Writer.TryWrite(notification);

    public IAsyncEnumerable<QueuedNotification> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
