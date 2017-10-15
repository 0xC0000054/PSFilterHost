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

using PSFilterHostDll.PSApi.PICA;
using System;

namespace PSFilterHostDll.PSApi
{
	internal sealed class PICASuites : IDisposable
	{
		private PICABufferSuite bufferSuite;
		private PICAColorSpaceSuite colorSpaceSuite;
		private PICAUIHooksSuite uiHooksSuite;
		private string pluginName;
		private ASZStringSuite zstringSuite;

		public PICASuites()
		{
			this.bufferSuite = null;
			this.colorSpaceSuite = null;
			this.uiHooksSuite = null;
			this.pluginName = string.Empty;
			this.zstringSuite = null;
		}

		/// <summary>
		/// Sets the name of the plugin used by the <see cref="PSUIHooksSuite1.GetPluginName"/> callback.
		/// </summary>
		/// <param name="name">The name of the plugin.</param>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
		public void SetPluginName(string name)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}

			this.pluginName = name;
		}

		public ASZStringSuite ASZStringSuite
		{
			get
			{
				if (zstringSuite == null)
				{
					zstringSuite = new ASZStringSuite();
				}

				return zstringSuite;
			}
		}

		public PSBufferSuite1 CreateBufferSuite1()
		{
			if (bufferSuite == null)
			{
				this.bufferSuite = new PICABufferSuite();
			}

			return this.bufferSuite.CreateBufferSuite1();
		}

		public PSColorSpaceSuite1 CreateColorSpaceSuite1()
		{
			if (colorSpaceSuite == null)
			{
				this.colorSpaceSuite = new PICAColorSpaceSuite(this.ASZStringSuite);
			}

			return this.colorSpaceSuite.CreateColorSpaceSuite1();
		} 

		public static unsafe PSHandleSuite1 CreateHandleSuite1(HandleProcs* procs)
		{
			return PICAHandleSuite.CreateHandleSuite1(procs);
		}

		public static unsafe PSHandleSuite2 CreateHandleSuite2(HandleProcs* procs)
		{
			return PICAHandleSuite.CreateHandleSuite2(procs);
		}

		public static unsafe PropertyProcs CreatePropertySuite(PropertyProcs* procs)
		{
			PropertyProcs suite = new PropertyProcs
			{
				propertyProcsVersion = procs->propertyProcsVersion,
				numPropertyProcs = procs->numPropertyProcs,
				getPropertyProc = procs->getPropertyProc,
				setPropertyProc = procs->setPropertyProc
			};

			return suite;
		}

		public unsafe PSUIHooksSuite1 CreateUIHooksSuite1(FilterRecord* filterRecord)
		{
			if (uiHooksSuite == null)
			{
				this.uiHooksSuite = new PICAUIHooksSuite(filterRecord, this.pluginName, this.ASZStringSuite);
			}

			return this.uiHooksSuite.CreateUIHooksSuite1(filterRecord);
		}

#if PICASUITEDEBUG
		public static unsafe SPPluginsSuite4 CreateSPPlugs4()
		{
			return PICASPPluginsSuite.CreateSPPluginsSuite4();
		} 
#endif

		public void Dispose()
		{
			if (bufferSuite != null)
			{
				this.bufferSuite.Dispose();
				this.bufferSuite = null;
			}
		}
	}
}
