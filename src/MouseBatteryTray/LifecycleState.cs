namespace MouseBatteryTray;

internal enum ReceiverState { Present, Absent, Unknown }
internal enum RecoveryWork { None, Verify, Refresh }

/// <summary>UI-thread state; time is supplied in monotonic milliseconds.</summary>
internal sealed class LifecycleState
{
    internal int Generation { get; private set; }
    internal bool Stale { get; private set; }
    internal bool Closed { get; private set; }
    internal ReceiverState Receiver { get; private set; } = ReceiverState.Unknown;
    private long? verifyAt, refreshAt;
    private bool verifying;

    internal void QueueInitialRead(long now)
    {
        if (!Closed && !Pending && !verifying) refreshAt = now;
    }

    internal void Resume(long now)
    {
        if (Closed) return;
        Generation++; Stale = true;
        // A resume during receiver verification must not bypass revalidation.
        if (verifyAt.HasValue || verifying) { verifyAt = now + 3000; refreshAt = null; }
        else refreshAt = now + 3000;
    }
    internal void DeviceChanged(long now)
    {
        if (Closed) return;
        Generation++; Stale = true; verifyAt = now + 3000; refreshAt = null;
    }
    internal RecoveryWork TakeWork(long now, bool busy)
    {
        if (Closed || busy) return RecoveryWork.None;
        if (verifyAt is long v) { if (now < v) return RecoveryWork.None; verifyAt = null; verifying = true; return RecoveryWork.Verify; }
        if (refreshAt is long r) { if (now < r) return RecoveryWork.None; refreshAt = null; return RecoveryWork.Refresh; }
        return RecoveryWork.None;
    }
    internal bool Pending => verifyAt.HasValue || refreshAt.HasValue;
    internal void Verified(ReceiverState state, int generation, long now)
    {
        verifying = false;
        if (Closed || generation != Generation) return;
        Receiver = state;
        if (state == ReceiverState.Present) refreshAt = now + 3000;
        else
        {
            Stale = true;
            refreshAt = null;
            if (state == ReceiverState.Unknown) verifyAt = now + 60000;
        }
    }
    internal bool Accept(int generation) => !Closed && generation == Generation && !verifyAt.HasValue;
    internal void ReadingAccepted() { if (!Closed) { Stale = false; Receiver = ReceiverState.Present; } }
    internal void Shutdown() { Closed = true; Generation++; verifyAt = refreshAt = null; }
}
