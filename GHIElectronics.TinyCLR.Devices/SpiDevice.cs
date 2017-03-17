using System;
using System.Runtime.CompilerServices;

namespace GHIElectronics.TinyCLR.Devices.Spi {
    // warning CS0414: The field 'Windows.Devices.Spi.SpiDevice.xxx' is assigned but its value is never used
    //                 - These are all used in native code methods.
#pragma warning disable 0414

    public sealed class SpiDevice : IDisposable {
        private static string s_SpiPrefix = "SPI";

        private readonly string m_deviceId;
        private readonly SpiConnectionSettings m_settings;

        private bool m_disposed = false;
        private int m_mskPin = -1;
        private int m_misoPin = -1;
        private int m_mosiPin = -1;
        private int m_spiBus = -1;

        /// <summary>
        /// Initializes a new instance of SpiDevice.
        /// </summary>
        /// <param name="deviceId">The unique name of the device.</param>
        /// <param name="settings">Settings to open the device with.</param>
        internal SpiDevice(string deviceId, SpiConnectionSettings settings) {
            // Device ID must match the index in device information.
            // We don't have many buses, so just hard-code the valid ones instead of parsing.
            this.m_spiBus = GetBusNum(deviceId);
            this.m_deviceId = deviceId.Substring(0);
            this.m_settings = new SpiConnectionSettings(settings);

            InitNative();
        }

        ~SpiDevice() {
            Dispose(false);
        }

        /// <summary>
        /// Gets the unique ID associated with the device.
        /// </summary>
        /// <value>The ID.</value>
        public string DeviceId {
            get {
                if (this.m_disposed) {
                    throw new ObjectDisposedException();
                }

                return this.m_deviceId.Substring(0);
            }
        }

        /// <summary>
        /// Gets the connection settings for the device.
        /// </summary>
        /// <value>The connection settings.</value>
        public SpiConnectionSettings ConnectionSettings {
            get {
                if (this.m_disposed) {
                    throw new ObjectDisposedException();
                }

                // We must return a copy so the caller can't accidentally mutate our internal settings.
                return new SpiConnectionSettings(this.m_settings);
            }
        }

        /// <summary>
        /// Gets all the SPI buses found on the system.
        /// </summary>
        /// <returns>String containing all the buses found on the system.</returns>
        public static string GetDeviceSelector() => s_SpiPrefix;

        /// <summary>
        /// Gets all the SPI buses found on the system that match the input parameter.
        /// </summary>
        /// <param name="friendlyName">Input parameter specifying an identifying name for the desired bus. This usually
        ///     corresponds to a name on the schematic.</param>
        /// <returns>String containing all the buses that have the input in the name.</returns>
        public static string GetDeviceSelector(string friendlyName) => friendlyName;

        /// <summary>
        /// Retrieves the info about a certain bus.
        /// </summary>
        /// <param name="busId">The id of the bus.</param>
        /// <returns>The bus info requested.</returns>
        public static SpiBusInfo GetBusInfo(string busId) {
            var busNum = GetBusNum(busId);
            return new SpiBusInfo(busNum);
        }

        /// <summary>
        /// Opens a device with the connection settings provided.
        /// </summary>
        /// <param name="busId">The id of the bus.</param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static SpiDevice FromId(string busId, SpiConnectionSettings settings) {
            // FUTURE: This should be "Task<SpiDevice*> FromIdAsync(...)"
            switch (settings.Mode) {
                case SpiMode.Mode0:
                case SpiMode.Mode1:
                case SpiMode.Mode2:
                case SpiMode.Mode3:
                    break;

                default:
                    throw new ArgumentException();
            }

            switch (settings.SharingMode) {
                case SpiSharingMode.Exclusive:
                case SpiSharingMode.Shared:
                    break;

                default:
                    throw new ArgumentException();
            }

            switch (settings.DataBitLength) {
                case 8:
                case 16:
                    break;

                default:
                    throw new ArgumentException();
            }

            return new SpiDevice(busId, settings);
        }

        /// <summary>
        /// Writes to the connected device.
        /// </summary>
        /// <param name="buffer">Array containing the data to write to the device.</param>
        public void Write(byte[] buffer) {
            if (buffer == null) {
                throw new ArgumentException();
            }

            TransferInternal(buffer, null, false);
        }

        /// <summary>
        /// Reads from the connected device.
        /// </summary>
        /// <param name="readBuffer">Array containing data read from the device.</param>
        public void Read(byte[] buffer) {
            if (buffer == null) {
                throw new ArgumentException();
            }

            TransferInternal(null, buffer, false);
        }

        /// <summary>
        /// Transfer data sequentially to the device.
        /// </summary>
        /// <param name="writeBuffer">Array containing data to write to the device.</param>
        /// <param name="readBuffer">Array containing data read from the device.</param>
        public void TransferSequential(byte[] writeBuffer, byte[] readBuffer) {
            if ((writeBuffer == null) || (readBuffer == null)) {
                throw new ArgumentException();
            }

            TransferInternal(writeBuffer, readBuffer, false);
        }

        /// <summary>
        /// Transfer data using a full duplex communication system. Full duplex allows both the master and the slave to
        /// communicate simultaneously.
        /// </summary>
        /// <param name="writeBuffer">Array containing data to write to the device.</param>
        /// <param name="readBuffer">Array containing data read from the device.</param>
        public void TransferFullDuplex(byte[] writeBuffer, byte[] readBuffer) {
            if ((writeBuffer == null) || (readBuffer == null)) {
                throw new ArgumentException();
            }

            TransferInternal(writeBuffer, readBuffer, true);
        }

        /// <summary>
        /// Closes the connection to the device.
        /// </summary>
        public void Dispose() {
            if (!this.m_disposed) {
                Dispose(true);
                GC.SuppressFinalize(this);
                this.m_disposed = true;
            }
        }

        internal static string[] GetValidBusNames() => new string[] {
                s_SpiPrefix + "1",
                s_SpiPrefix + "2",
                s_SpiPrefix + "3",
                s_SpiPrefix + "4",
            };

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern private void InitNative();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern private void DisposeNative();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern private void TransferInternal(byte[] writeBuffer, byte[] readBuffer, bool fullDuplex);

        private static int GetBusNum(string deviceId) {
            if (deviceId == null) throw new ArgumentNullException(nameof(deviceId));
            if (deviceId.Length < 4 || deviceId.IndexOf("SPI") != 0 || !int.TryParse(deviceId.Substring(3), out var id) || id <= 0) throw new ArgumentException("Invalid SPI bus", nameof(deviceId));

            return id - 1;
        }

        /// <summary>
        /// Releases internal resources held by the device.
        /// </summary>
        /// <param name="disposing">True if called from Dispose, false if called from the finalizer.</param>
        private void Dispose(bool disposing) {
            if (disposing) {
                DisposeNative();
            }
        }
    }
}
