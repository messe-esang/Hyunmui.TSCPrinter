using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using TSCSDK;
using Xunit;

namespace Hyunmui.TSCPrinter.Tests
{
    public class UsbRawWriterTests
    {
        [Fact]
        public void ImmediateSuccessUsesActualCountsAndAnOwnedCopy()
        {
            var bytes = new byte[] { 1, 2, 3, 4 };
            var native = new Kernel();
            native.Writes.Enqueue(new UsbIoResult(true, 2, 0));
            native.Writes.Enqueue(new UsbIoResult(true, 2, 0));
            native.OnWrite = () => Array.Fill(bytes, (byte)9);
            Assert.Equal(4, new UsbRawWriter(native).Write((IntPtr)73, bytes));
            Assert.Equal(new byte[] { 1, 2, 3, 4, 3, 4 }, native.Observed);
            Assert.Equal(2, native.Closed);
            Assert.Equal(0, native.WaitCalls);
            Assert.All(native.Handles, handle => Assert.Equal((IntPtr)73, handle));
        }

        [Fact]
        public void PendingPartialCompletionContinuesOnTheCapturedHandle()
        {
            var native = new Kernel();
            native.Writes.Enqueue(new UsbIoResult(false, 0, 997));
            native.Writes.Enqueue(new UsbIoResult(true, 1, 0));
            native.Results.Enqueue(new UsbIoResult(true, 2, 0));
            Assert.Equal(3, new UsbRawWriter(native).Write((IntPtr)81, new byte[] { 1, 2, 3 }));
            Assert.Equal(new byte[] { 1, 2, 3, 3 }, native.Observed);
            Assert.All(native.Handles, handle => Assert.Equal((IntPtr)81, handle));
            Assert.Equal(2, native.Closed);
        }

        [Theory]
        [InlineData(0u)]
        [InlineData(4u)]
        public void InvalidImmediateCountDoesNotLoop(uint count)
        {
            var native = new Kernel();
            native.Writes.Enqueue(new UsbIoResult(true, count, 0));
            Assert.Contains("invalid byte count", Assert.Throws<TscException>(() => new UsbRawWriter(native).Write((IntPtr)1, new byte[3])).Message);
            Assert.Equal(1, native.Closed);
            Assert.Equal(0, native.WaitCalls);
        }

        [Fact]
        public void ImmediateErrorDoesNotEnterPendingWait()
        {
            var native = new Kernel();
            native.Writes.Enqueue(new UsbIoResult(false, 0, 5));
            Assert.Equal("WriteFile failed with native error 5", Assert.Throws<TscException>(() => new UsbRawWriter(native).Write((IntPtr)1, new byte[3])).Message);
            Assert.Equal(0, native.WaitCalls);
            Assert.Equal(0, native.CancelCalls);
            Assert.Equal(1, native.Closed);
        }

        [Theory]
        [InlineData(true, 0)]
        [InlineData(false, 1168)]
        [InlineData(false, 6)]
        public void TimeoutDrainsEvenWhenCancelFailsAndPreservesTimeout(bool cancelSuccess, int error)
        {
            var native = new Kernel { Cancellation = new UsbIoResult(cancelSuccess, 0, error) };
            native.Writes.Enqueue(new UsbIoResult(false, 0, 997));
            native.Waits.Enqueue(258);
            native.Waits.Enqueue(0);
            native.Results.Enqueue(new UsbIoResult(false, 0, 995));
            Assert.Equal("WAIT_TIMEOUT", Assert.Throws<TscException>(() => new UsbRawWriter(native).Write((IntPtr)97, new byte[] { 7, 8 })).Message);
            Assert.Equal(new uint[] { 2000, uint.MaxValue }, native.WaitDurations);
            Assert.Equal(1, native.CancelCalls);
            Assert.Equal(1, native.Closed);
            Assert.All(native.Handles, handle => Assert.Equal((IntPtr)97, handle));
        }

        [Fact]
        public void CompletionErrorReleasesOnlyAfterSignal()
        {
            var native = new Kernel();
            native.Writes.Enqueue(new UsbIoResult(false, 0, 997));
            native.Results.Enqueue(new UsbIoResult(false, 0, 31));
            Assert.Equal("GetOverlappedResult failed with native error 31", Assert.Throws<TscException>(() => new UsbRawWriter(native).Write((IntPtr)1, new byte[1])).Message);
            Assert.Equal(1, native.Closed);
        }

        [Fact]
        public void EmptyInputDoesNotAcquireResources()
        {
            var native = new Kernel();
            Assert.Equal(0, new UsbRawWriter(native).Write((IntPtr)1, Array.Empty<byte>()));
            Assert.Equal(0, native.Created);
        }

        [Fact]
        public void OverlappedLayoutMatchesTheCurrentPointerWidth()
        {
            Assert.Equal(IntPtr.Size == 8 ? 32 : 20, Marshal.SizeOf<UsbOverlapped>());
            Assert.Equal(IntPtr.Size * 2 + 8, Marshal.OffsetOf<UsbOverlapped>(nameof(UsbOverlapped.EventHandle)).ToInt32());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void UnprovenCompletionQuarantinesStorageAndFaultsOnlyThisWriter(bool inconsistentSignal)
        {
            var native = new Kernel();
            native.Writes.Enqueue(new UsbIoResult(false, 0, 997));
            native.Waits.Enqueue(258);
            native.Waits.Enqueue(inconsistentSignal ? 0 : uint.MaxValue);
            if (inconsistentSignal) native.Results.Enqueue(new UsbIoResult(false, 0, 996));
            var writer = new UsbRawWriter(native);
            var failure = Assert.Throws<TscException>(() => writer.Write((IntPtr)7, new byte[] { 42 }));
            Assert.Equal("WAIT_TIMEOUT", failure.Message);
            Assert.Contains("retained", Assert.IsType<string>(failure.Data["UsbWriteCompletion"]));
            Assert.Equal(0, native.Closed);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            native.AssertRequestAlive(42);
            Assert.Contains("faulted", Assert.Throws<TscException>(() => writer.Write((IntPtr)7, new byte[] { 9 })).Message);
            Assert.Equal(1, native.Created);
            var independent = new Kernel();
            independent.Writes.Enqueue(new UsbIoResult(true, 1, 0));
            Assert.Equal(1, new UsbRawWriter(independent).Write((IntPtr)8, new byte[] { 4 }));
        }

        [Fact]
        public void WaitFailureIsPreservedAfterCancellationAndDrain()
        {
            var native = new Kernel();
            native.Writes.Enqueue(new UsbIoResult(false, 0, 997));
            native.Waits.Enqueue(uint.MaxValue);
            native.Waits.Enqueue(0);
            native.Results.Enqueue(new UsbIoResult(false, 0, 995));
            Assert.Equal("WaitForSingleObject failed with native error 6", Assert.Throws<TscException>(() => new UsbRawWriter(native).Write((IntPtr)9, new byte[1])).Message);
            Assert.Equal(1, native.Closed);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DrainExceptionsPreserveOriginalTimeoutAndQuarantine(bool duringCancel)
        {
            var secondary = new InvalidOperationException("adapter failure");
            var native = new Kernel { CancelFailure = duringCancel ? secondary : null, DrainFailure = duringCancel ? null : secondary };
            native.Writes.Enqueue(new UsbIoResult(false, 0, 997));
            native.Waits.Enqueue(258);
            var writer = new UsbRawWriter(native);
            var failure = Assert.Throws<TscException>(() => writer.Write((IntPtr)1, new byte[] { 4 }));
            Assert.Equal("WAIT_TIMEOUT", failure.Message);
            Assert.Same(secondary, failure.Data["UsbWriteDrainError"]);
            Assert.Equal(0, native.Closed);
            Assert.Contains("faulted", Assert.Throws<TscException>(() => writer.Write((IntPtr)1, new byte[1])).Message);
            Assert.Equal(1, native.Created);
        }

        [Fact]
        public void InflightPartialLoopStopsBeforeAllocatingAfterAnotherCallFaults()
        {
            var native = new Kernel();
            native.Writes.Enqueue(new UsbIoResult(true, 1, 0));
            native.Writes.Enqueue(new UsbIoResult(false, 0, 997));
            native.Waits.Enqueue(258);
            native.Waits.Enqueue(uint.MaxValue);
            var writer = new UsbRawWriter(native);
            native.OnClose = () => Assert.Equal("WAIT_TIMEOUT", Assert.Throws<TscException>(() => writer.Write((IntPtr)2, new byte[] { 1 })).Message);
            Assert.Contains("faulted", Assert.Throws<TscException>(() => writer.Write((IntPtr)1, new byte[] { 1, 2 })).Message);
            Assert.Equal(2, native.Created);
            Assert.Equal(1, native.Closed);
        }

        [Theory]
        [InlineData(0u)]
        [InlineData(4u)]
        public void PendingCompletionRequiresPositiveBoundedCount(uint count)
        {
            var native = new Kernel();
            native.Writes.Enqueue(new UsbIoResult(false, 0, 997));
            native.Results.Enqueue(new UsbIoResult(true, count, 0));
            Assert.Contains("invalid byte count", Assert.Throws<TscException>(() => new UsbRawWriter(native).Write((IntPtr)1, new byte[3])).Message);
            Assert.Equal(1, native.Closed);
        }

        [Fact]
        public void EventCreationFailureDoesNotStartWrite()
        {
            var native = new Kernel { FailCreate = true };
            Assert.Equal("CreateEvent failed with native error 8", Assert.Throws<TscException>(() => new UsbRawWriter(native).Write((IntPtr)1, new byte[1])).Message);
            Assert.Empty(native.Handles);
            Assert.Equal(0, native.Closed);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EventCloseFailureIsReportedWithoutMaskingThePrimaryFailure(bool writeFailed)
        {
            var secondary = new InvalidOperationException("close failed");
            var native = new Kernel { CloseFailure = secondary };
            native.Writes.Enqueue(new UsbIoResult(!writeFailed, 1, writeFailed ? 5 : 0));
            var writer = new UsbRawWriter(native);
            if (writeFailed)
            {
                var failure = Assert.Throws<TscException>(() => writer.Write((IntPtr)1, new byte[1]));
                Assert.Equal("WriteFile failed with native error 5", failure.Message);
                Assert.Same(secondary, failure.Data["UsbWriteCloseError"]);
            }
            else Assert.Same(secondary, Assert.Throws<InvalidOperationException>(() => writer.Write((IntPtr)1, new byte[1])));
        }

        private sealed class Kernel : IUsbWriteKernel
        {
            internal readonly Queue<UsbIoResult> Writes = new();
            internal readonly Queue<UsbIoResult> Results = new();
            internal readonly Queue<uint> Waits = new();
            internal readonly List<byte> Observed = new();
            internal readonly List<IntPtr> Handles = new();
            internal readonly List<uint> WaitDurations = new();
            internal Action? OnWrite;
            internal Action? OnClose;
            internal Exception? CancelFailure;
            internal Exception? CloseFailure;
            internal bool FailCreate;
            internal Exception? DrainFailure;
            private readonly HashSet<IntPtr> closedEvents = new();
            internal UsbIoResult Cancellation = new(true, 0, 0);
            internal int Created;
            internal int Closed;
            internal int WaitCalls;
            internal int CancelCalls;
            private IntPtr currentBuffer;
            private IntPtr currentOverlapped;
            private bool pending;

            internal void AssertRequestAlive(byte expected)
            {
                Assert.Equal(expected, Marshal.ReadByte(currentBuffer));
                Assert.Equal((IntPtr)(Created + 100), Marshal.PtrToStructure<UsbOverlapped>(currentOverlapped).EventHandle);
            }

            public IntPtr CreateEvent(out int error)
            {
                error = FailCreate ? 8 : 0;
                if (FailCreate) return IntPtr.Zero;
                return (IntPtr)(++Created + 100);
            }

            public UsbIoResult Write(IntPtr handle, IntPtr buffer, uint count, IntPtr overlapped)
            {
                Handles.Add(handle);
                currentBuffer = buffer;
                currentOverlapped = overlapped;
                var layout = Marshal.PtrToStructure<UsbOverlapped>(overlapped);
                Assert.Equal(UIntPtr.Zero, layout.Internal);
                Assert.Equal(UIntPtr.Zero, layout.InternalHigh);
                Assert.Equal(0u, layout.Offset);
                Assert.Equal(0u, layout.OffsetHigh);
                Assert.Equal((IntPtr)(Created + 100), layout.EventHandle);
                OnWrite?.Invoke();
                var copy = new byte[count];
                Marshal.Copy(buffer, copy, 0, copy.Length);
                Observed.AddRange(copy);
                var result = Writes.Dequeue();
                pending = !result.Success && result.Error == 997;
                return result;
            }

            public uint Wait(IntPtr eventHandle, uint milliseconds, out int error)
            {
                WaitCalls++;
                WaitDurations.Add(milliseconds);
                error = 6;
                Assert.True(pending);
                Assert.DoesNotContain(eventHandle, closedEvents);
                if (milliseconds == uint.MaxValue && DrainFailure != null) throw DrainFailure;
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Assert.Equal(Observed[0], Marshal.ReadByte(currentBuffer));
                Assert.Equal(eventHandle, Marshal.PtrToStructure<UsbOverlapped>(currentOverlapped).EventHandle);
                var result = Waits.Count == 0 ? 0 : Waits.Dequeue();
                if (result == 0) pending = false;
                return result;
            }

            public UsbIoResult Complete(IntPtr handle, IntPtr overlapped)
            {
                Handles.Add(handle);
                Assert.False(pending);
                Assert.Equal(currentOverlapped, overlapped);
                return Results.Dequeue();
            }

            public UsbIoResult Cancel(IntPtr handle, IntPtr overlapped)
            {
                Handles.Add(handle);
                Assert.True(pending);
                Assert.Equal(currentOverlapped, overlapped);
                CancelCalls++;
                if (CancelFailure != null) throw CancelFailure;
                return Cancellation;
            }

            public void CloseEvent(IntPtr eventHandle)
            {
                Assert.False(pending);
                Closed++;
                closedEvents.Add(eventHandle);
                var callback = OnClose;
                OnClose = null;
                callback?.Invoke();
                if (CloseFailure != null) throw CloseFailure;
            }
        }
    }
}
