/////////////////////////////////////////////////////////////////////////////////
//
// Adobe(R) Photoshop(R) filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2013 Nicholas Hayes
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
        private string backingFile;
        private HistoryChunk chunk;
        private HistoryItemState state;

        public CanvasHistoryState CanvasHistory
        {
            get
            {
                return chunk.canvas;
            }
        }

        public BitmapSource Image
        {
            get
            {
                return chunk.image;
            }
        }
       
        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryItem"/> class.
        /// </summary>
        public HistoryItem(CanvasHistoryState historyCanvas, BitmapSource currentImage)
        {
            this.backingFile = Path.GetTempFileName();
            this.state = HistoryItemState.Memory;

            this.chunk = new HistoryChunk(historyCanvas, currentImage);

            this.ToDisk();
        }

        /// <summary>
        /// Serializes the HistoryChunk to disk.
        /// </summary>
        public void ToDisk()
        {
            if (state == HistoryItemState.Memory)
            {
                using (FileStream fs = new FileStream(this.backingFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(fs, this.chunk);
                    this.state = HistoryItemState.Disk;
                    this.chunk.Dispose();
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
                using (FileStream fs = new FileStream(this.backingFile, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    this.chunk = (HistoryChunk)bf.Deserialize(fs);
                    this.state = HistoryItemState.Memory;
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
            Dispose(true);
        }
        private void Dispose(bool disposing)
        {
            if (!disposed && disposing)
            {
                File.Delete(this.backingFile);
                this.chunk.Dispose();
                this.state = HistoryItemState.Disposed;
                this.disposed = true;
            }

        }
        #endregion
        
        [Serializable]
        private sealed class HistoryChunk : IDisposable, ISerializable
        {
            internal CanvasHistoryState canvas;
            internal BitmapSource image;

            public HistoryChunk(CanvasHistoryState canvasHistory, BitmapSource source)
            {
                this.canvas = canvasHistory;
                this.image = source;
            }

            private HistoryChunk(SerializationInfo info, StreamingContext context)
            {
                this.canvas = (CanvasHistoryState)info.GetValue("canvas", typeof(CanvasHistoryState));

                byte[] temp = (byte[])info.GetValue("image", typeof(byte[]));

                using (MemoryStream stream = new MemoryStream(temp))
                {
                    PngBitmapDecoder dec = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    this.image = dec.Frames[0].Clone();
                }
            }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("canvas", this.canvas, typeof(CanvasHistoryState));

                using (MemoryStream stream = new MemoryStream())
                {
                    PngBitmapEncoder enc = new PngBitmapEncoder();
                    enc.Frames.Add(BitmapFrame.Create(this.image));
                    enc.Save(stream);

                    info.AddValue("image", stream.GetBuffer(), typeof(byte[]));
                }

            }

            private bool disposed;
            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            private void Dispose(bool disposing)
            {
                if (!disposed && disposing)
                {
                    this.disposed = true;
                    if (canvas != null)
                    {
                        this.canvas.Dispose();
                        this.canvas = null;
                    } 
                }

            }
        }
    }
}
