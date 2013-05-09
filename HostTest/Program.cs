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
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace HostTest
{
	static class Program
	{
		static class NativeMethods
		{
			[DllImport("kernel32.dll", EntryPoint = "SetProcessDEPPolicy")]
			[return: MarshalAs(UnmanagedType.Bool)]
			internal static extern bool SetProcessDEPPolicy(uint dwFlags);
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
#if DEBUG
			bool res = NativeMethods.SetProcessDEPPolicy(0U);
			System.Diagnostics.Debug.WriteLine("SetProcessDEPPolicy returned " + res);
#else
			NativeMethods.SetProcessDEPPolicy(0U); // Opt-out of DEP as many filters crash with it on.
#endif


			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Form1());
		}
	}
}
