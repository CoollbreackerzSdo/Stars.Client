using System.Diagnostics;
using System.Threading;

namespace Stars.Client.Features.Playback;

/// <summary>
/// Adaptive jitter buffer for encoded Opus frames.
///
/// Network packets do not arrive perfectly evenly; playback must. This buffer pre-rolls a
/// few frames and then releases exactly one frame per playback tick. Its depth adapts to the
/// measured inter-arrival jitter: more jitter → deeper buffer (more resilience), stable link
/// → shallower buffer (less latency).
/// </summary>
public sealed class JitterBuffer
{
    private const double FrameMs = 20.0;

    private readonly Lock _gate = new();
    private readonly Queue<byte[]> _queue = new();
    private readonly int _minDepth;
    private readonly int _maxDepth;

    private int _targetDepth;
    private long _lastArrivalTimestamp;
    private double _jitterMs;
    private double _peakJitterMs;
    private long _received;
    private long _underruns;
    private long _dropped;

    public JitterBuffer(int minDepth = 2, int maxDepth = 10)
    {
        _minDepth = Math.Max(1, minDepth);
        _maxDepth = Math.Max(_minDepth, maxDepth);
        _targetDepth = _minDepth;
    }

    /// <summary>Frames currently buffered.</summary>
    public int Count
    {
        get
        {
            lock (_gate)
                return _queue.Count;
        }
    }

    /// <summary>Number of frames playback waits for before starting (the pre-roll).</summary>
    public int TargetDepth
    {
        get
        {
            lock (_gate)
                return _targetDepth;
        }
    }

    public int BufferMilliseconds => TargetDepth * (int)FrameMs;

    public double JitterMs
    {
        get
        {
            lock (_gate)
                return _jitterMs;
        }
    }

    public long Underruns => Interlocked.Read(ref _underruns);

    public long Dropped => Interlocked.Read(ref _dropped);

    public long Received => Interlocked.Read(ref _received);

    /// <summary>True once enough frames are buffered to begin playback.</summary>
    public bool IsPrimed
    {
        get
        {
            lock (_gate)
                return _queue.Count >= _targetDepth;
        }
    }

    public void Reset()
    {
        lock (_gate)
        {
            _queue.Clear();
            _lastArrivalTimestamp = 0;
            _jitterMs = 0;
            _peakJitterMs = 0;
            _targetDepth = _minDepth;
        }

        Interlocked.Exchange(ref _underruns, 0);
        Interlocked.Exchange(ref _dropped, 0);
        Interlocked.Exchange(ref _received, 0);
    }

    /// <summary>Adds a freshly received packet, updating the jitter estimate and depth.</summary>
    public void Push(byte[] packet)
    {
        lock (_gate)
        {
            var now = Stopwatch.GetTimestamp();
            if (_lastArrivalTimestamp != 0)
            {
                var deltaMs = (now - _lastArrivalTimestamp) * 1000.0 / Stopwatch.Frequency;
                var deviation = Math.Abs(deltaMs - FrameMs);
                // Exponentially weighted moving average of the inter-arrival deviation.
                _jitterMs = _jitterMs * 0.9 + deviation * 0.1;
                // Peak with slow decay: keeps the buffer deep enough after a burst.
                _peakJitterMs = Math.Max(_peakJitterMs * 0.995, _jitterMs);
            }

            _lastArrivalTimestamp = now;
            Interlocked.Increment(ref _received);

            _queue.Enqueue(packet);

            // Hard cap: never let latency grow unbounded. Prefer dropping the OLDEST frame
            // (keeps playback current) but only when we truly overran the ceiling.
            while (_queue.Count > _maxDepth)
            {
                _queue.Dequeue();
                Interlocked.Increment(ref _dropped);
            }

            // Target depth covers the worst recent jitter plus a small safety margin.
            var estimate = Math.Max(_jitterMs, _peakJitterMs);
            var desired = (int)Math.Ceiling(estimate / FrameMs) + 3;
            _targetDepth = Math.Clamp(desired, _minDepth, _maxDepth);

            // Discard stale frames when the target shrinks or packets arrive in a burst.
            while (_queue.Count > _targetDepth)
            {
                _queue.Dequeue();
                Interlocked.Increment(ref _dropped);
            }
        }
    }

    /// <summary>How many frames are buffered right now.</summary>
    public int Available
    {
        get
        {
            lock (_gate)
                return _queue.Count;
        }
    }

    /// <summary>Removes one frame for playback. Returns false on an underrun.</summary>
    public bool TryDequeue(out byte[] packet)
    {
        lock (_gate)
        {
            if (_queue.Count == 0)
            {
                packet = [];
                Interlocked.Increment(ref _underruns);
                return false;
            }

            packet = _queue.Dequeue();
            return true;
        }
    }
}
