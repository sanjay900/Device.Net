﻿using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Device.Net
{
    public abstract class DeviceBase
    {
        #region Fields
        private SemaphoreSlim _WriteAndReadLock = new SemaphoreSlim(1, 1);
        private bool disposed = false;
        public const string DeviceDisposedErrorMessage = "This device has already been disposed";
        protected string LogRegion;
        #endregion

        #region Public Abstract Properties
        public abstract ushort WriteBufferSize { get; }
        public abstract ushort ReadBufferSize { get; }
        public abstract bool IsInitialized { get; }
        #endregion

        #region Public Properties
        public ITracer Tracer { get; set; }
        public ConnectedDeviceDefinitionBase ConnectedDeviceDefinition { get; set; }
        public string DeviceId { get; set; }
        public ILogger Logger { get; set; }
        #endregion

        #region Private Methods
        private void Log(string message, string region, Exception ex, LogLevel logLevel)
        {
            Logger?.Log(message, region, ex, logLevel);
        }
        #endregion

        #region Protected Methods
        protected void Log(string message, [CallerMemberName] string callMemberName = null)
        {
            Log(message, null, LogLevel.Information, callMemberName);
        }

        protected void Log(string message, Exception ex, [CallerMemberName] string callMemberName = null)
        {
            Log(message, ex, LogLevel.Error, callMemberName);
        }

        protected void Log(string message, Exception ex, LogLevel logLevel, [CallerMemberName] string callMemberName = null)
        {
            if (LogRegion == null)
            {
                LogRegion = GetType().Name;
            }

            Log(message, $"{LogRegion} - {callMemberName}", ex, logLevel);
        }
        #endregion

        #region Public Abstract Methods
        public abstract Task<byte[]> ReadAsync();
        public abstract Task WriteAsync(byte[] data);
        #endregion

        #region Public Methods
        public async Task<byte[]> WriteAndReadAsync(byte[] writeBuffer)
        {
            await _WriteAndReadLock.WaitAsync();

            try
            {
                await WriteAsync(writeBuffer);
                return await ReadAsync();
            }
            finally
            {
                _WriteAndReadLock.Release();
            }
        }

        /// <summary> 
        /// Many Hid devices on Windows have a buffer size that is one byte larger than the logical buffer size. For compatibility with other platforms etc. we need to remove the first byte. See DataHasExtraByte
        /// </summary> 
        public static byte[] RemoveFirstByte(byte[] bytes)
        {
            var length = bytes.Length - 1;
            var retVal = new byte[length];

            Array.Copy(bytes, 1, retVal, 0, length);

            return retVal;
        }

        public virtual void Dispose()
        {
            if (disposed) return;

            disposed = true;

            _WriteAndReadLock.Dispose();
        }
        #endregion
    }
}
