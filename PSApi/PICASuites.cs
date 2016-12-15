/////////////////////////////////////////////////////////////////////////////////
//
// 8bf filter host for .NET
// http://psfilterhost.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2012-2016 Nicholas Hayes
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
#if PICASUITEDEBUG
		private PICAColorSpaceSuite colorSpaceSuite;
#endif
		private PICAUIHooksSuite uiHooksSuite;
		private string pluginName;

		public PICASuites()
		{
			this.bufferSuite = null;
#if PICASUITEDEBUG
			this.colorSpaceSuite = null;
#endif
			this.uiHooksSuite = null;
			this.pluginName = string.Empty;
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

		public static ASZStringSuite1 CreateASZStringSuite1()
		{
			return ASZStringSuite.Instance.CreateASZStringSuite1();
		}

		public PSBufferSuite1 CreateBufferSuite1()
		{
			if (bufferSuite == null)
			{
				this.bufferSuite = new PICABufferSuite();
			}

			return this.bufferSuite.CreateBufferSuite1();
		}

#if PICASUITEDEBUG
		public PSColorSpaceSuite1 CreateColorSpaceSuite1()
		{
			if (colorSpaceSuite == null)
			{
				this.colorSpaceSuite = new PICAColorSpaceSuite();
			}

			return this.colorSpaceSuite.CreateColorSpaceSuite1();
		} 
#endif

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
			PropertyProcs suite = new PropertyProcs();
			suite.propertyProcsVersion = procs->propertyProcsVersion;
			suite.numPropertyProcs = procs->numPropertyProcs;
			suite.getPropertyProc = procs->getPropertyProc;
			suite.setPropertyProc = procs->setPropertyProc;

			return suite;
		}

		public unsafe PSUIHooksSuite1 CreateUIHooksSuite1(FilterRecord* filterRecord)
		{
			if (uiHooksSuite == null)
			{
				this.uiHooksSuite = new PICAUIHooksSuite(filterRecord, this.pluginName);
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
