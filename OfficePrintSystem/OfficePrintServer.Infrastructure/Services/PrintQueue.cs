using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using OfficePrintServer.Core.Entities;
using OfficePrintServer.Core.Interfaces;

namespace OfficePrintServer.Infrastructure.Services;

public class PrintQueue : IPrintQueue
{
    private readonly Channel<PrintJob> _channel;

    public PrintQueue()
    {
        _channel = Channel.CreateUnbounded<PrintJob>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public async Task EnqueueJobAsync(PrintJob job)
    {
        await _channel.Writer.WriteAsync(job);
    }

    public async Task<PrintJob?> DequeueJobAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _channel.Reader.ReadAsync(cancellationToken);
        }
        catch (ChannelClosedException)
        {
            return null;
        }
    }
}