﻿#region License
// Copyright (c) 2019 Michael T. Russin
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace MtrDev.WebView2.Wrapper
{
    // The class ManagedIStream is not COM-visible. Its purpose is to be able to invoke COM interfaces
    // from managed code rather than the contrary.
    internal class ManagedIStream : IStream
    {
        public const int stc4 = 0x0443,
            SHGFP_TYPE_CURRENT = 0,
            STGM_READ = 0x00000000,
            STGM_WRITE = 0x00000001,
            STGM_READWRITE = 0x00000002,
            STGM_SHARE_EXCLUSIVE = 0x00000010,
            STGM_CREATE = 0x00001000,
            STGM_TRANSACTED = 0x00010000,
            STGM_CONVERT = 0x00020000,
            STGM_DELETEONRELEASE = 0x04000000,
            STREAM_SEEK_SET = 0x0,
            STREAM_SEEK_CUR = 0x1,
            STREAM_SEEK_END = 0x2,
            STGTY_STORAGE = 1,
            STGTY_STREAM = 2,
            STGTY_LOCKBYTES = 3,
            STGTY_PROPERTY = 4;

        /// <summary>
        /// Constructor
        /// </summary>
        internal ManagedIStream(Stream ioStream)
        {
            if (ioStream == null)
            {
                throw new ArgumentNullException("ioStream");
            }
            _ioStream = ioStream;
        }

        /// <summary>
        /// Read at most bufferSize bytes into buffer and return the effective
        /// number of bytes read in bytesReadPtr (unless null).
        /// </summary>
        /// <remarks>
        /// mscorlib disassembly shows the following MarshalAs parameters
        /// void Read([Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1)] byte[] pv, int cb, IntPtr pcbRead);
        /// This means marshaling code will have found the size of the array buffer in the parameter bufferSize.
        /// </remarks>
        ///<SecurityNote>
        ///     Critical: calls Marshal.WriteInt32 which LinkDemands, takes pointers as input
        ///</SecurityNote>
        [SecurityCritical]
        void IStream.Read(Byte[] buffer, Int32 bufferSize, IntPtr bytesReadPtr)
        {
            Int32 bytesRead = _ioStream.Read(buffer, 0, (int)bufferSize);
            if (bytesReadPtr != IntPtr.Zero)
            {
                Marshal.WriteInt32(bytesReadPtr, bytesRead);
            }
        }

        /// <summary>
        /// Move the stream pointer to the specified position.
        /// </summary>
        /// <remarks>
        /// System.IO.stream supports searching past the end of the stream, like
        /// OLE streams.
        /// newPositionPtr is not an out parameter because the method is required
        /// to accept NULL pointers.
        /// </remarks>
        ///<SecurityNote>
        ///     Critical: calls Marshal.WriteInt64 which LinkDemands, takes pointers as input
        ///</SecurityNote>
        [SecurityCritical]
        void IStream.Seek(Int64 offset, Int32 origin, IntPtr newPositionPtr)
        {
            SeekOrigin seekOrigin;

            // The operation will generally be I/O bound, so there is no point in
            // eliminating the following switch by playing on the fact that
            // System.IO uses the same integer values as IStream for SeekOrigin.
            switch (origin)
            {
                case STREAM_SEEK_SET:
                    seekOrigin = SeekOrigin.Begin;
                    break;
                case STREAM_SEEK_CUR:
                    seekOrigin = SeekOrigin.Current;
                    break;
                case STREAM_SEEK_END:
                    seekOrigin = SeekOrigin.End;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("origin");
            }
            long position = _ioStream.Seek(offset, seekOrigin);

            // Dereference newPositionPtr and assign to the pointed location.
            if (newPositionPtr != IntPtr.Zero)
            {
                Marshal.WriteInt64(newPositionPtr, position);
            }
        }

        /// <summary>
        /// Sets stream's size.
        /// </summary>
        void IStream.SetSize(Int64 libNewSize)
        {
            _ioStream.SetLength(libNewSize);
        }

        /// <summary>
        /// Obtain stream stats.
        /// </summary>
        /// <remarks>
        /// STATSG has to be qualified because it is defined both in System.Runtime.InteropServices and
        /// System.Runtime.InteropServices.ComTypes.
        /// The STATSTG structure is shared by streams, storages and byte arrays. Members irrelevant to streams
        /// or not available from System.IO.Stream are not returned, which leaves only cbSize and grfMode as 
        /// meaningful and available pieces of information.
        /// grfStatFlag is used to indicate whether the stream name should be returned and is ignored because
        /// this information is unavailable.
        /// </remarks>
        void IStream.Stat(out System.Runtime.InteropServices.ComTypes.STATSTG streamStats, int grfStatFlag)
        {
            streamStats = new System.Runtime.InteropServices.ComTypes.STATSTG();
            streamStats.type = STGTY_STREAM;
            streamStats.cbSize = _ioStream.Length;

            // Return access information in grfMode.
            streamStats.grfMode = 0; // default value for each flag will be false
            if (_ioStream.CanRead && _ioStream.CanWrite)
            {
                streamStats.grfMode |= STGM_READWRITE;
            }
            else if (_ioStream.CanRead)
            {
                streamStats.grfMode |= STGM_READ;
            }
            else if (_ioStream.CanWrite)
            {
                streamStats.grfMode |= STGM_WRITE;
            }
            else
            {
                // A stream that is neither readable nor writable is a closed stream.
                // Note the use of an exception that is known to the interop marshaller
                // (unlike ObjectDisposedException).
                throw new IOException("Stream Object Disposed");
            }
        }

        /// <summary>
        /// Write at most bufferSize bytes from buffer.
        /// </summary>
        ///<SecurityNote>
        ///     Critical: calls Marshal.WriteInt32 which LinkDemands, takes pointers as input
        ///</SecurityNote>
        [SecurityCritical]
        void IStream.Write(Byte[] buffer, Int32 bufferSize, IntPtr bytesWrittenPtr)
        {
            _ioStream.Write(buffer, 0, bufferSize);
            if (bytesWrittenPtr != IntPtr.Zero)
            {
                // If fewer than bufferSize bytes had been written, an exception would
                // have been thrown, so it can be assumed we wrote bufferSize bytes.
                Marshal.WriteInt32(bytesWrittenPtr, bufferSize);
            }
        }

        #region Unimplemented methods
        /// <summary>
        /// Create a clone.
        /// </summary>
        /// <remarks>
        /// Not implemented.
        /// </remarks>
        void IStream.Clone(out IStream streamCopy)
        {
            streamCopy = null;
            throw new NotSupportedException();
        }

        /// <summary>
        /// Read at most bufferSize bytes from the receiver and write them to targetStream.
        /// </summary>
        /// <remarks>
        /// Not implemented.
        /// </remarks>
        void IStream.CopyTo(IStream targetStream, Int64 bufferSize, IntPtr buffer, IntPtr bytesWrittenPtr)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Commit changes.
        /// </summary>
        /// <remarks>
        /// Only relevant to transacted streams.
        /// </remarks>
        void IStream.Commit(Int32 flags)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Lock at most byteCount bytes starting at offset.
        /// </summary>
        /// <remarks>
        /// Not supported by System.IO.Stream.
        /// </remarks>
        void IStream.LockRegion(Int64 offset, Int64 byteCount, Int32 lockType)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Undo writes performed since last Commit.
        /// </summary>
        /// <remarks>
        /// Relevant only to transacted streams.
        /// </remarks>
        void IStream.Revert()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Unlock the specified region.
        /// </summary>
        /// <remarks>
        /// Not supported by System.IO.Stream.
        /// </remarks>
        void IStream.UnlockRegion(Int64 offset, Int64 byteCount, Int32 lockType)
        {
            throw new NotSupportedException();
        }
        #endregion Unimplemented methods

        #region Fields
        private Stream _ioStream;
        #endregion Fields
    }
}
