﻿/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2022 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Media.Imaging;

namespace HostTest
{
    /// <summary>
    /// The class that encapsulates the undo / redo history.
    /// </summary>
    internal sealed class HistoryItem : IDisposable
    {
        private readonly string backingFile;
        private HistoryChunk chunk;
        private HistoryItemState state;

        public CanvasHistoryState CanvasHistory => chunk.canvas;

        public BitmapSource Image => chunk.image;

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryItem"/> class.
        /// </summary>
        /// <param name="file">The backing file.</param>
        /// <param name="historyCanvas">The history canvas.</param>
        /// <param name="currentImage">The current image.</param>
        public HistoryItem(string file, CanvasHistoryState historyCanvas, BitmapSource currentImage)
        {
            backingFile = file;
            state = HistoryItemState.Memory;
            chunk = new HistoryChunk(historyCanvas, currentImage);

            ToDisk();
        }

        /// <summary>
        /// Serializes the HistoryChunk to disk.
        /// </summary>
        public void ToDisk()
        {
            if (state == HistoryItemState.Memory)
            {
                using (FileStream fs = new FileStream(backingFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(fs, chunk);
                    state = HistoryItemState.Disk;
                    chunk.Dispose();
                }
            }
        }

        /// <summary>
        /// Retrieves the HistoryChunk from disk.
        /// </summary>
        public void ToMemory()
        {
            if (state == HistoryItemState.Disk)
            {
                using (FileStream fs = new FileStream(backingFile, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    chunk = (HistoryChunk)bf.Deserialize(fs);
                    state = HistoryItemState.Memory;
                }
            }
        }

        #region IDisposible Members
        private bool disposed;
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                File.Delete(backingFile);
                chunk.Dispose();
                state = HistoryItemState.Disposed;
                disposed = true;
            }
        }
        #endregion

        [Serializable]
        private sealed class HistoryChunk : IDisposable, ISerializable
        {
            internal CanvasHistoryState canvas;
            internal BitmapSource image;
            private bool disposed;

            public HistoryChunk(CanvasHistoryState canvasHistory, BitmapSource source)
            {
                canvas = canvasHistory;
                image = source;
                disposed = false;
            }

            private HistoryChunk(SerializationInfo info, StreamingContext context)
            {
                canvas = (CanvasHistoryState)info.GetValue("canvas", typeof(CanvasHistoryState));

                byte[] temp = (byte[])info.GetValue("image", typeof(byte[]));

                using (MemoryStream stream = new MemoryStream(temp))
                {
                    BitmapFrame frame = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    image = new WriteableBitmap(frame); // Copy the frame using a WriteableBitmap to fix the threading issue with the BitmapFrameDecoder.
                    image.Freeze();
                }
            }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("canvas", canvas, typeof(CanvasHistoryState));

                using (MemoryStream stream = new MemoryStream())
                {
                    // Set the meta data manually as some codecs may not implement all the properties required for BitmapMetadata.Clone() to succeed.
                    BitmapMetadata metadata = null;

                    try
                    {
                        metadata = image.Metadata as BitmapMetadata;
                    }
                    catch (NotSupportedException)
                    {
                    }

                    PngBitmapEncoder enc = new PngBitmapEncoder();
                    enc.Frames.Add(BitmapFrame.Create(image, null, metadata, null));
                    enc.Save(stream);

                    info.AddValue("image", stream.GetBuffer(), typeof(byte[]));
                }
            }

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            {
                if (!disposed)
                {
                    disposed = true;
                    if (canvas != null)
                    {
                        canvas.Dispose();
                        canvas = null;
                    }
                }
            }
        }
    }
}
