/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// https://github.com/0xC0000054/PSFilterHost
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2017 Nicholas Hayes
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
			this.historyList = new List<HistoryItem>();
			this.index = -1;
			this.disposed = false;
		}

		/// <summary>
		/// Adds the history item to the undo stack.
		/// </summary>
		/// <param name="historyState">The current <see cref="HostTest.CanvasHistoryState"/>.</param>
		/// <param name="image">The current destination image.</param>
		public void AddHistoryItem(CanvasHistoryState historyState, BitmapSource image)
		{
			if (this.index < (this.historyList.Count - 1))
			{
				this.historyList.RemoveRange(this.index + 1, (this.historyList.Count - 1) - this.index);
			}

			this.historyList.Add(new HistoryItem(historyState, image));
			this.index = this.historyList.Count - 1;

			OnHistoryChanged();
		}

		public int Count
		{
			get
			{
				return this.historyList.Count;
			}
		}

		/// <summary>
		/// Clears the undo history.
		/// </summary>
		public void Clear()
		{
			int count = this.historyList.Count;

			for (int i = 0; i < count; i++)
			{
				this.historyList[i].Dispose();
			}

			this.historyList.Clear();
			this.index = -1;

			OnHistoryChanged();
		}

		/// <summary>
		/// Steps the back to the previous state.
		/// </summary>
		/// <param name="surface">The canvas to step back.</param>
		/// <param name="image">The destination image for the current item.</param>
		public void StepBackward(Canvas surface, ref BitmapSource image)
		{
			if (this.CanUndo)
			{
				this.index--;

				this.historyList[this.index].ToMemory();

				surface.CopyFromHistoryState(this.historyList[this.index].CanvasHistory);
				image = this.historyList[this.index].Image;

				this.historyList[this.index].ToDisk();

				surface.IsDirty = this.index > 0;
			}
		}

		/// <summary>
		/// Steps the <see cref="HostTest.Canvas"/> forward to the next state.
		/// </summary>
		/// <param name="surface">The canvas to step forward.</param>
		public void StepForward(Canvas surface, ref BitmapSource image)
		{
			if (this.CanRedo)
			{
				this.index++;

				this.historyList[this.index].ToMemory();

				surface.CopyFromHistoryState(this.historyList[this.index].CanvasHistory);
				image = this.historyList[this.index].Image;

				this.historyList[this.index].ToDisk();

				surface.IsDirty = true;
			}
		}

		public bool CanUndo
		{
			get
			{
				return (this.index > 0);
			}
		}

		public bool CanRedo
		{
			get
			{
				return (this.index < (this.historyList.Count - 1));
			}
		}

		private void OnHistoryChanged()
		{
			EventHandler historyChanged = this.HistoryChanged;

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
				for (int i = 0; i < this.historyList.Count; i++)
				{
					this.historyList[i].Dispose();
				}
				this.historyList = null;
				this.disposed = true;
			}
		}
		#endregion
	}
}
