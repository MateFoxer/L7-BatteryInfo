namespace MouseBatteryTray.Hid.Tests;

public class LifecycleTests
{
    [Fact] public void ResumeMakesReadingStaleAndDelaysRefresh()
    {
        var s = new LifecycleState(); s.ReadingAccepted(); s.Resume(100);
        Assert.True(s.Stale); Assert.Equal(RecoveryWork.None, s.TakeWork(3099, false));
        Assert.Equal(RecoveryWork.Refresh, s.TakeWork(3100, false));
    }
    [Fact] public void DuplicateResumeCoalesces()
    {
        var s = new LifecycleState(); s.Resume(0); s.Resume(1000);
        Assert.Equal(RecoveryWork.None, s.TakeWork(3000, false)); Assert.Equal(RecoveryWork.Refresh, s.TakeWork(4000, false)); Assert.Equal(RecoveryWork.None, s.TakeWork(4001, false));
    }
    [Fact] public void RemovalAndReconnection()
    {
        var s = new LifecycleState(); s.DeviceChanged(0); Assert.Equal(RecoveryWork.Verify, s.TakeWork(3000, false));
        s.Verified(ReceiverState.Absent, s.Generation, 3000); Assert.Equal(ReceiverState.Absent, s.Receiver); Assert.True(s.Stale);
        s.DeviceChanged(4000); Assert.Equal(RecoveryWork.Verify, s.TakeWork(7000, false));
        s.Verified(ReceiverState.Present, s.Generation, 7000); Assert.Equal(RecoveryWork.Refresh, s.TakeWork(10000, false));
    }
    [Fact] public void RapidReconnectStillSchedulesRefresh()
    {
        var s = new LifecycleState(); s.ReadingAccepted(); s.DeviceChanged(0); s.DeviceChanged(1000);
        Assert.Equal(RecoveryWork.None, s.TakeWork(3000, false)); Assert.Equal(RecoveryWork.Verify, s.TakeWork(4000, false));
        s.Verified(ReceiverState.Present, s.Generation, 4000); Assert.True(s.Stale);
        Assert.Equal(RecoveryWork.Refresh, s.TakeWork(7000, false)); Assert.Equal(RecoveryWork.None, s.TakeWork(7001, false));
    }
    [Fact] public void InFlightReadAndOldVerificationAreRejected()
    {
        var s = new LifecycleState(); int old = s.Generation; s.DeviceChanged(0);
        Assert.False(s.Accept(old)); Assert.Equal(RecoveryWork.None, s.TakeWork(3000, true));
        s.Verified(ReceiverState.Present, old, 3000); Assert.Equal(ReceiverState.Unknown, s.Receiver);
        Assert.Equal(RecoveryWork.Verify, s.TakeWork(3000, false));
    }
    [Fact] public void UnknownRemainsStaleAndRetriesConservatively()
    {
        var s = new LifecycleState(); s.ReadingAccepted(); s.DeviceChanged(0); s.TakeWork(3000, false);
        s.Verified(ReceiverState.Unknown, s.Generation, 3000); Assert.True(s.Stale); Assert.Equal(ReceiverState.Unknown, s.Receiver);
        Assert.Equal(RecoveryWork.None, s.TakeWork(62999, false)); Assert.Equal(RecoveryWork.Verify, s.TakeWork(63000, false));
    }
    [Fact] public void ReconnectWhileVerificationPendingInvalidatesOldResult()
    {
        var s = new LifecycleState(); s.DeviceChanged(0); s.TakeWork(3000, false); int old = s.Generation;
        s.DeviceChanged(3100); s.Verified(ReceiverState.Absent, old, 3200); Assert.Equal(ReceiverState.Unknown, s.Receiver);
        Assert.Equal(RecoveryWork.Verify, s.TakeWork(6100, false));
    }
    [Fact] public void ShutdownDropsPendingWorkAndResults()
    {
        var s = new LifecycleState(); s.Resume(0); int old = s.Generation; s.Shutdown(); s.DeviceChanged(10);
        Assert.False(s.Accept(old)); Assert.Equal(RecoveryWork.None, s.TakeWork(100000, false));
    }
    [Fact] public void LifecycleDoesNotResetAlertSuppression()
    {
        var a = new AlertSettings(); Assert.Equal(10, a.Observe(9, true));
        var s = new LifecycleState(); s.Resume(0); s.DeviceChanged(1); s.Shutdown();
        Assert.True(a.LowSent); Assert.True(a.CriticalSent); Assert.Null(a.Observe(9, true));
    }
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void FailedVerificationCancelsPendingResumeRefresh(int value)
    {
        var result = (ReceiverState)value;
        var s = new LifecycleState();
        s.Resume(0);
        s.Verified(result, s.Generation, 100);
        Assert.True(s.Stale);
        Assert.Equal(RecoveryWork.None, s.TakeWork(3000, false));
        Assert.Equal(result, s.Receiver);
        if (result == ReceiverState.Unknown) Assert.Equal(RecoveryWork.Verify, s.TakeWork(60100, false));
    }
    [Fact] public void ResumeDuringVerificationRequiresNewVerification()
    {
        var s = new LifecycleState(); s.DeviceChanged(0);
        Assert.Equal(RecoveryWork.Verify, s.TakeWork(3000, false));
        int old = s.Generation; s.Resume(3100);
        s.Verified(ReceiverState.Present, old, 3200);
        Assert.Equal(RecoveryWork.None, s.TakeWork(6099, false));
        Assert.Equal(RecoveryWork.Verify, s.TakeWork(6100, false));
        s.Verified(ReceiverState.Present, s.Generation, 6100);
        Assert.Equal(RecoveryWork.Refresh, s.TakeWork(9100, false));
        Assert.Equal(RecoveryWork.None, s.TakeWork(9101, false));
    }
    [Fact] public void ResumeWhileDeviceCheckQueuedDoesNotBypassIt()
    {
        var s = new LifecycleState(); s.DeviceChanged(0); s.Resume(1000);
        Assert.Equal(RecoveryWork.None, s.TakeWork(3000, false));
        Assert.Equal(RecoveryWork.Verify, s.TakeWork(4000, false));
    }
    [Fact] public void InitialStateAllowsFirstReadAndAcceptsConnectedResult()
    {
        var s = new LifecycleState();
        Assert.False(s.Pending); Assert.Equal(ReceiverState.Unknown, s.Receiver);
        Assert.True(s.Accept(s.Generation)); s.ReadingAccepted();
        Assert.Equal(ReceiverState.Present, s.Receiver); Assert.False(s.Stale);
    }
    [Fact] public void StartupWithoutReceiverCanBeConfirmedAbsent()
    {
        var s = new LifecycleState(); s.DeviceChanged(0);
        Assert.Equal(RecoveryWork.Verify, s.TakeWork(3000, false));
        s.Verified(ReceiverState.Absent, s.Generation, 3000);
        Assert.True(s.Stale); Assert.Equal(ReceiverState.Absent, s.Receiver);
        Assert.False(s.Pending);
    }
    [Fact] public void ResumeDuringReadRejectsOldResultAndQueuesOneRefresh()
    {
        var s = new LifecycleState(); int old = s.Generation; s.Resume(0); s.Resume(500);
        Assert.False(s.Accept(old)); Assert.Equal(RecoveryWork.None, s.TakeWork(3500, true));
        Assert.Equal(RecoveryWork.Refresh, s.TakeWork(3500, false));
        Assert.Equal(RecoveryWork.None, s.TakeWork(3501, false));
    }
    [Fact] public void StartupQueuesExactlyOneRead()
    {
        var s = new LifecycleState(); s.QueueInitialRead(0);
        Assert.Equal(RecoveryWork.Refresh, s.TakeWork(0, false));
        Assert.Equal(RecoveryWork.None, s.TakeWork(1, false));
        Assert.True(s.Accept(s.Generation));
    }
    [Fact] public void StartupWaitsForPendingVerification()
    {
        var s = new LifecycleState(); s.DeviceChanged(0); s.QueueInitialRead(1);
        Assert.Equal(RecoveryWork.None, s.TakeWork(2999, false));
        Assert.Equal(RecoveryWork.Verify, s.TakeWork(3000, false));
        s.QueueInitialRead(3001);
        s.Verified(ReceiverState.Present, s.Generation, 3001);
        Assert.Equal(RecoveryWork.Refresh, s.TakeWork(6001, false));
        Assert.Equal(RecoveryWork.None, s.TakeWork(6002, false));
    }
    [Fact] public void StartupReadInvalidatedByDeviceChangeRecovers()
    {
        var s = new LifecycleState(); s.QueueInitialRead(0);
        Assert.Equal(RecoveryWork.Refresh, s.TakeWork(0, false)); int old = s.Generation;
        s.DeviceChanged(1); Assert.False(s.Accept(old));
        Assert.Equal(RecoveryWork.Verify, s.TakeWork(3001, false));
        s.Verified(ReceiverState.Present, s.Generation, 3001);
        Assert.Equal(RecoveryWork.Refresh, s.TakeWork(6001, false));
    }
    [Fact] public void StartupWithoutReceiverWaitsForReconnect()
    {
        var s = new LifecycleState(); s.QueueInitialRead(0); s.DeviceChanged(1);
        Assert.Equal(RecoveryWork.Verify, s.TakeWork(3001, false));
        s.Verified(ReceiverState.Absent, s.Generation, 3001);
        Assert.Equal(RecoveryWork.None, s.TakeWork(6001, false));
        s.DeviceChanged(7000); Assert.Equal(RecoveryWork.Verify, s.TakeWork(10000, false));
        s.Verified(ReceiverState.Present, s.Generation, 10000);
        Assert.Equal(RecoveryWork.Refresh, s.TakeWork(13000, false));
    }
    [Fact] public void StartupUnknownRetriesVerificationConservatively()
    {
        var s = new LifecycleState(); s.QueueInitialRead(0); s.DeviceChanged(0);
        Assert.Equal(RecoveryWork.Verify, s.TakeWork(3000, false));
        s.Verified(ReceiverState.Unknown, s.Generation, 3000);
        Assert.Equal(RecoveryWork.None, s.TakeWork(62999, false));
        Assert.Equal(RecoveryWork.Verify, s.TakeWork(63000, false));
        s.Verified(ReceiverState.Present, s.Generation, 63000);
        Assert.Equal(RecoveryWork.Refresh, s.TakeWork(66000, false));
    }
    [Fact] public void StartupWorkDoesNotRunWhileBusyOrAfterShutdown()
    {
        var s = new LifecycleState(); s.QueueInitialRead(0);
        Assert.Equal(RecoveryWork.None, s.TakeWork(0, true));
        s.Shutdown(); Assert.Equal(RecoveryWork.None, s.TakeWork(1, false));
    }
}
