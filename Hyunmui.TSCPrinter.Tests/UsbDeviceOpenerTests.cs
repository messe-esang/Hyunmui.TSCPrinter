using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using TSCSDK;
using Xunit;

namespace Hyunmui.TSCPrinter.Tests
{
    public class UsbDeviceOpenerTests
    {
        [Theory]
        [InlineData(false, 0u)]
        [InlineData(true, 0x40000080u)]
        public void UnicodePathOpensOnlyAfterAllDiscoveryResourcesAreReleased(bool overlapped, uint flags)
        {
            var fake = new Native();
            Assert.True(fake.Opener.TryOpen(overlapped, out var handle));
            Assert.Equal(71, handle);
            Assert.Equal(fake.Path, fake.OpenedPath);
            Assert.Equal(flags, fake.Flags);
            Assert.Equal(new[] { "get", "enum", "probe", "allocate", "detail", "free", "destroy", "open" }, fake.Calls);
            Assert.Equal(0, fake.OutstandingBuffers);
        }

        [Theory]
        [InlineData("get")]
        [InlineData("enum")]
        [InlineData("probe")]
        [InlineData("detail")]
        public void NativeDiscoveryFailureDoesNotOpenDeviceAndReleasesAcquiredResources(string stage)
        {
            var fake = new Native { FailStage = stage };
            Assert.False(fake.Opener.TryOpen(false, out _));
            Assert.Null(fake.OpenedPath);
            Assert.Equal(stage == "get" ? 0 : 1, fake.DestroyCalls);
            Assert.Equal(stage == "detail" ? 1 : 0, fake.FreeCalls);
            Assert.Equal(0, fake.OutstandingBuffers);
        }

        [Theory]
        [InlineData(0u)]
        [InlineData(2u)]
        [InlineData(5u)]
        [InlineData(9u)]
        [InlineData(uint.MaxValue)]
        public void InvalidProbeSizeDoesNotAllocate(uint size)
        {
            var fake = new Native { ProbeSize = size };
            Assert.False(fake.Opener.TryOpen(false, out _));
            Assert.DoesNotContain("allocate", fake.Calls);
            Assert.Equal(1, fake.DestroyCalls);
        }

        [Fact]
        public void UnexpectedSuccessfulProbeDoesNotAllocate()
        {
            var fake = new Native { ProbeSuccess = true };
            Assert.False(fake.Opener.TryOpen(false, out _));
            Assert.DoesNotContain("allocate", fake.Calls);
            Assert.Equal(1, fake.DestroyCalls);
        }

        [Theory]
        [InlineData(0u)]
        [InlineData(5u)]
        [InlineData(uint.MaxValue)]
        public void InvalidReturnedSizeNeverReadsThePath(uint size)
        {
            var fake = new Native { ReturnedSize = size };
            Assert.False(fake.Opener.TryOpen(false, out _));
            Assert.Null(fake.OpenedPath);
            Assert.Equal(1, fake.FreeCalls);
            Assert.Equal(1, fake.DestroyCalls);
        }

        [Theory]
        [InlineData("")]
        [InlineData("unterminated")]
        public void EmptyOrUnterminatedPathIsRejected(string path)
        {
            var fake = new Native { Path = path, Terminate = path.Length == 0 };
            Assert.False(fake.Opener.TryOpen(false, out _));
            Assert.Null(fake.OpenedPath);
            Assert.Equal(1, fake.FreeCalls);
            Assert.Equal(1, fake.DestroyCalls);
        }

        [Fact]
        public void ReturnedSizeBoundsTheTerminatorSearch()
        {
            var fake = new Native { Path = "abcdef", ReturnedSize = 8 };
            Assert.False(fake.Opener.TryOpen(false, out _));
            Assert.Null(fake.OpenedPath);
        }

        [Theory]
        [InlineData("allocate")]
        [InlineData("detail")]
        [InlineData("free")]
        [InlineData("destroy")]
        public void ManagedFailuresReleaseRemainingResourcesBeforePropagating(string stage)
        {
            var failure = new InvalidOperationException(stage);
            var fake = new Native { ThrowStage = stage, Failure = failure };
            Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => fake.Opener.TryOpen(false, out _)));
            Assert.Null(fake.OpenedPath);
            Assert.Equal(1, fake.DestroyCalls);
            Assert.Equal(0, fake.OutstandingBuffers);
        }

        [Fact]
        public void NativeDestroyFailureIsExplicitAndDoesNotOpenDevice()
        {
            var fake = new Native { FailStage = "destroy" };
            Assert.Equal("SetupDiDestroyDeviceInfoList failed with native error 5",
                Assert.Throws<TscException>(() => fake.Opener.TryOpen(false, out _)).Message);
            Assert.Null(fake.OpenedPath);
            Assert.Equal(1, fake.FreeCalls);
        }

        [Fact]
        public void DestroyFailureDoesNotMaskPrimaryException()
        {
            var failure = new InvalidOperationException("detail failed");
            var fake = new Native { ThrowStage = "detail", Failure = failure, FailStage = "destroy" };
            Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => fake.Opener.TryOpen(false, out _)));
            Assert.IsType<TscException>(failure.Data["UsbDiscoveryCleanupError"]);
            Assert.Equal(1, fake.FreeCalls);
        }

        [Fact]
        public void CreateFileFailureIsAnAttemptWithLegacyInvalidHandle()
        {
            var fake = new Native { DeviceHandle = -1 };
            Assert.True(fake.Opener.TryOpen(false, out var handle));
            Assert.Equal(-1, handle);
            Assert.Equal(1, fake.FreeCalls);
            Assert.Equal(1, fake.DestroyCalls);
        }

        [Fact]
        public void InterleavedDiscoveryCallsKeepTheirOwnPathsAndLists()
        {
            var first = new Native { Path = "first" };
            var second = new Native { Path = "second", Devices = (IntPtr)92 };
            first.OnDetail = () => Assert.True(second.Opener.TryOpen(true, out _));
            Assert.True(first.Opener.TryOpen(false, out _));
            Assert.Equal("first", first.OpenedPath);
            Assert.Equal("second", second.OpenedPath);
            Assert.Equal(1, first.DestroyCalls);
            Assert.Equal(1, second.DestroyCalls);
        }

        [Fact]
        public void NullAllocationReleasesDeviceListWithoutWritingMemory()
        {
            var fake = new Native();
            var opener = new UsbDeviceOpener(fake, _ => IntPtr.Zero, _ => throw new InvalidOperationException("Unexpected free"));
            Assert.Throws<OutOfMemoryException>(() => opener.TryOpen(false, out _));
            Assert.Equal(1, fake.DestroyCalls);
            Assert.Null(fake.OpenedPath);
        }

        [Fact]
        public void DeviceOpenExceptionOccursAfterDiscoveryCleanup()
        {
            var failure = new InvalidOperationException("open failed");
            var fake = new Native { ThrowStage = "open", Failure = failure };
            Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => fake.Opener.TryOpen(false, out _)));
            Assert.Equal(1, fake.FreeCalls);
            Assert.Equal(1, fake.DestroyCalls);
        }

        [Fact]
        public void NativeLayoutUsesPointerWidthAndUnicodeHeaderSizes()
        {
            Assert.Equal(6, UsbDeviceOpener.DetailHeaderSize(4));
            Assert.Equal(8, UsbDeviceOpener.DetailHeaderSize(8));
            Assert.Equal(IntPtr.Size == 8 ? 32 : 28, Marshal.SizeOf<UsbDeviceInterface>());
            Assert.Equal(24, Marshal.OffsetOf<UsbDeviceInterface>(nameof(UsbDeviceInterface.Reserved)).ToInt32());
            Assert.Throws<NotSupportedException>(() => UsbDeviceOpener.DetailHeaderSize(16));
        }

        internal sealed class Native : IUsbDiscoveryNative
        {
            internal readonly List<string> Calls = new();
            internal string Path = @"\\?\usb#장치#arbitrary-symbolic-name";
            internal string? FailStage;
            internal string? ThrowStage;
            internal Exception Failure = new InvalidOperationException();
            internal string? OpenedPath;
            internal uint Flags;
            internal uint? ProbeSize;
            internal uint? ReturnedSize;
            internal bool ProbeSuccess;
            internal bool Terminate = true;
            internal int DeviceHandle = 71;
            internal IntPtr Devices = (IntPtr)91;
            internal int DestroyCalls;
            internal int FreeCalls;
            internal int OutstandingBuffers;
            internal Action? OnDetail;
            internal UsbDeviceOpener Opener => new(this, Allocate, Free);

            private void Visit(string stage)
            {
                Calls.Add(stage);
                if (ThrowStage == stage) throw Failure;
            }

            private IntPtr Allocate(int size)
            {
                Visit("allocate");
                var result = Marshal.AllocCoTaskMem(size);
                OutstandingBuffers++;
                return result;
            }

            private void Free(IntPtr buffer)
            {
                Marshal.FreeCoTaskMem(buffer);
                OutstandingBuffers--;
                FreeCalls++;
                Visit("free");
            }

            public IntPtr GetDevices(out int error)
            {
                Visit("get");
                error = 5;
                return FailStage == "get" ? (IntPtr)(-1) : Devices;
            }

            public bool GetFirstInterface(IntPtr devices, ref UsbDeviceInterface data, out int error)
            {
                Assert.Equal(Devices, devices);
                Assert.Equal((uint)Marshal.SizeOf<UsbDeviceInterface>(), data.Size);
                Visit("enum");
                error = 5;
                return FailStage != "enum";
            }

            public bool GetDetail(IntPtr devices, ref UsbDeviceInterface data, IntPtr buffer, uint capacity, out uint required, out int error)
            {
                Assert.Equal(Devices, devices);
                var stage = buffer == IntPtr.Zero ? "probe" : "detail";
                Visit(stage);
                var bytes = Encoding.Unicode.GetBytes(Path + (Terminate ? "\0" : ""));
                required = Math.Max((uint)(bytes.Length + 4), 8);
                if (stage == "probe")
                {
                    required = ProbeSize ?? required;
                    error = FailStage == stage ? 5 : 122;
                    return ProbeSuccess;
                }
                OnDetail?.Invoke();
                Assert.Equal(UsbDeviceOpener.DetailHeaderSize(IntPtr.Size), Marshal.ReadInt32(buffer));
                Assert.True(capacity >= bytes.Length + 4);
                Marshal.Copy(bytes, 0, IntPtr.Add(buffer, 4), bytes.Length);
                required = ReturnedSize ?? required;
                error = 5;
                return FailStage != stage;
            }

            public bool DestroyDevices(IntPtr devices, out int error)
            {
                Assert.Equal(Devices, devices);
                DestroyCalls++;
                Visit("destroy");
                error = 5;
                return FailStage != "destroy";
            }

            public int OpenDevice(string path, uint flags, out int error)
            {
                Assert.Equal(0, OutstandingBuffers);
                Assert.Equal(1, DestroyCalls);
                Visit("open");
                OpenedPath = path;
                Flags = flags;
                error = 5;
                return DeviceHandle;
            }
        }
    }

    [CollectionDefinition("USB discovery publication", DisableParallelization = true)]
    public class UsbDiscoveryCollection { }

    [Collection("USB discovery publication")]
    public class UsbDevicePublicationTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DiscoveryFailurePreservesPriorHandleButCreateFailurePublishesInvalid(bool overlapped)
        {
            var field = typeof(usb).GetField("HidHandle", BindingFlags.Static | BindingFlags.NonPublic)!;
            var saved = field.GetValue(null);
            try
            {
                field.SetValue(null, 123);
                var fake = new UsbDeviceOpenerTests.Native { FailStage = "enum" };
                var device = new usb(null!, null!, () => 123, fake.Opener);
                Assert.False(overlapped ? device.openport_overlapped() : device.openport());
                Assert.Equal(123, field.GetValue(null));
                // Fresh fake isolates resource counters between calls.
                fake = new UsbDeviceOpenerTests.Native { DeviceHandle = -1 };
                device = new usb(null!, null!, () => 123, fake.Opener);
                Assert.False(overlapped ? device.openport_overlapped() : device.openport());
                Assert.Equal(-1, field.GetValue(null));
            }
            finally { field.SetValue(null, saved); }
        }
    }
}
