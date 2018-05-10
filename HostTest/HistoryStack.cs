/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2018 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace HostTest
{
	/// <summary>
	/// This class stores the undo / redo history.
	/// </summary>
	internal sealed class HistoryStack : IDisposable
	{
		private List<HistoryItem> historyList;
		private int index;
		private bool disposed;

		public event EventHandler HistoryChanged;

		public HistoryStack()
		{
            historyList = new List<HistoryItem>();
            index = -1;
            disposed = false;
		}

		/// <summary>
		/// Adds the history item to the undo stack.
		/// </summary>
		/// <param name="historyState">The current <see cref="HostTest.CanvasHistoryState"/>.</param>
		/// <param name="image">The current destination image.</param>
		public void AddHistoryItem(CanvasHistoryState historyState, BitmapSource image)
		{
			if (index < (historyList.Count - 1))
			{
                historyList.RemoveRange(index + 1, (historyList.Count - 1) - index);
			}

            historyList.Add(new HistoryItem(historyState, image));
            index = historyList.Count - 1;

			OnHistoryChanged();
		}

		public int Count
		{
			get
			{
				return historyList.Count;
			}
		}

		/// <summary>
		/// Clears the undo history.
		/// </summary>
		public void Clear()
		{
			int count = historyList.Count;

			for (int i = 0; i < count; i++)
			{
                historyList[i].Dispose();
			}

            historyList.Clear();
            index = -1;

			OnHistoryChanged();
		}

		/// <summary>
		/// Steps the back to the previous state.
		/// </summary>
		/// <param name="surface">The canvas to step back.</param>
		/// <param name="image">The destination image for the current item.</param>
		public void StepBackward(Canvas surface, ref BitmapSource image)
		{
			if (CanUndo)
			{
                index--;

                historyList[index].ToMemory();

				surface.CopyFromHistoryState(historyList[index].CanvasHistory);
				image = historyList[index].Image;

                historyList[index].ToDisk();

				surface.IsDirty = index > 0;
			}
		}

		/// <summary>
		/// Steps the <see cref="HostTest.Canvas"/> forward to the next state.
		/// </summary>
		/// <param name="surface">The canvas to step forward.</param>
		public void StepForward(Canvas surface, ref BitmapSource image)
		{
			if (CanRedo)
			{
                index++;

                historyList[index].ToMemory();

				surface.CopyFromHistoryState(historyList[index].CanvasHistory);
				image = historyList[index].Image;

                historyList[index].ToDisk();

				surface.IsDirty = true;
			}
		}

		public bool CanUndo
		{
			get
			{
				return (index > 0);
			}
		}

		public bool CanRedo
		{
			get
			{
				return (index < (historyList.Count - 1));
			}
		}

		private void OnHistoryChanged()
		{
			EventHandler historyChanged = HistoryChanged;

			if (historyChanged != null)
			{
				historyChanged.Invoke(this, EventArgs.Empty);
			}
		}


		#region IDisposible Members
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			if (!disposed)
			{
				for (int i = 0; i < historyList.Count; i++)
				{
                    historyList[i].Dispose();
				}
                historyList = null;
                disposed = true;
			}
		}
		#endregion
	}
}
