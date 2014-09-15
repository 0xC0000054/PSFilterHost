/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2014 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Windows.Forms;
using PaintDotNet.SystemLayer;

namespace HostTest
{
	/// <summary>
	/// This class stops the sub-menu from expanding when the ToolStripMenuItem is disabled.
	/// </summary>
	internal sealed class ToolStripMenuItemEx : ToolStripMenuItem
	{
		private SubMenuItemCollection items;
		
		public ToolStripMenuItemEx(string text, ToolStripItem dropDownItem) : base(text)
		{
			this.items = new SubMenuItemCollection(this);
			this.items.Add(dropDownItem);
		}

		protected override void OnEnabledChanged(System.EventArgs e)
		{
			base.OnEnabledChanged(e);

			if (this.items != null)
			{
				if (base.Enabled)
				{
					if (base.DropDownItems.Count == 0)
					{
						ToolStripItem[] array = new ToolStripItem[items.Count];
						items.CopyTo(array, 0);

						base.DropDownItems.AddRange(array);
					}
				}
				else
				{
					if (base.DropDownItems.Count > 0)
					{
						base.DropDownItems.Clear();
					}
				}
			}
		}

		private static readonly int ArrowOffset2X = UI.ScaleWidth(2);
		private static readonly int ArrowOffset2Y = UI.ScaleHeight(2);
		private static readonly int ArrowOffset4Y = UI.ScaleHeight(4);

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			// When a menu is disabled we remove the DropDownItems and draw the arrow manually.
			if ((this.items != null) && !base.Enabled)
			{
				bool rightToLeft = base.RightToLeft == System.Windows.Forms.RightToLeft.Yes;
				
				int arrowWidth = 10;
				ArrowDirection direction = rightToLeft ? ArrowDirection.Left : ArrowDirection.Right;
 
				int textPadding = direction == ArrowDirection.Right ? 8 : 9; // See the TextPadding field in the System.Windows.Forms.ToolStripDropDownMenu static constructor.
 
				int arrowX = 0;

				if (rightToLeft)
				{
					arrowX = textPadding;
				}
				else
				{
					arrowX = (this.Bounds.Width - arrowWidth) - textPadding;
				}

				Rectangle arrowRect = new Rectangle(arrowX, 0, arrowWidth, this.Bounds.Height - this.Padding.Vertical);

				Point center = new Point(arrowRect.Left + (arrowRect.Width / 2), arrowRect.Top + (arrowRect.Height / 2));
				Point[] points = new Point[3];

				switch (direction)
				{
					case ArrowDirection.Left:
						points[0] = new Point(center.X + ArrowOffset2X, center.Y - ArrowOffset4Y);
						points[1] = new Point(center.X + ArrowOffset2X, center.Y + ArrowOffset4Y);
						points[2] = new Point(center.X - ArrowOffset2X, center.Y);
						break;
					case ArrowDirection.Right:
						points[0] = new Point(center.X - ArrowOffset2X, center.Y - ArrowOffset4Y);
						points[1] = new Point(center.X - ArrowOffset2X, center.Y + ArrowOffset4Y);
						points[2] = new Point(center.X + ArrowOffset2X, center.Y);
						break;
					case ArrowDirection.Up:
						points[0] = new Point(center.X - ArrowOffset2X, center.Y + 1);
						points[1] = new Point(center.X + ArrowOffset2X + 1, center.Y + 1);
						points[2] = new Point(center.X, center.Y - ArrowOffset2Y);
						break;
					default:
						points[0] = new Point(center.X - ArrowOffset2X, center.Y - 1);
						points[1] = new Point(center.X + ArrowOffset2X + 1, center.Y - 1);
						points[2] = new Point(center.X, center.Y + ArrowOffset2Y);
						break;
				}

				using (SolidBrush brush = new SolidBrush(SystemColors.ControlDark))
				{
					e.Graphics.FillPolygon(brush, points);
				}
			}
		}

		public SubMenuItemCollection SubMenuItems
		{
			get
			{
				return this.items;
			}
		}

		private void UpdateDropDownItems(ToolStripItem item)
		{
			if (base.Enabled)
			{
				base.DropDownItems.Add(item);
			}
		}

		private void UpdateSortedItems(List<ToolStripItem> sortedItems)
		{
			if (base.Enabled)
			{
				base.DropDownItems.Clear();
				base.DropDownItems.AddRange(sortedItems.ToArray());
			}
		}

		internal sealed class SubMenuItemCollection : Collection<ToolStripItem>
		{
			private ToolStripMenuItemEx owner;

			public SubMenuItemCollection(ToolStripMenuItemEx owner) : base(new List<ToolStripItem>())
			{
				this.owner = owner;
			}

			protected override void InsertItem(int index, ToolStripItem item)
			{
				base.InsertItem(index, item);

				if (owner != null)
				{
					owner.UpdateDropDownItems(item);
				}
			}

			public bool ContainsKey(string key)
			{
				if (!string.IsNullOrEmpty(key))
				{
					for (int i = 0; i < base.Items.Count; i++)
					{
						if (base.Items[i].Name == key)
						{
							return true;
						}
					}
				}

				return false;
			}

			public void Sort(IComparer<ToolStripItem> comparer)
			{
				List<ToolStripItem> items = (List<ToolStripItem>)base.Items;

				items.Sort(comparer);

				if (owner != null)
				{
					owner.UpdateSortedItems(items);
				}
			}
		}

	}
}
