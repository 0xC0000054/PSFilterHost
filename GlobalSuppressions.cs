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

// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.
//
// To add a suppression to this file, right-click the message in the 
// Error List, point to "Suppress Message(s)", and click 
// "In Project Suppression File".
// You do not need to add suppressions to this file manually.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1049:TypesThatOwnNativeResourcesShouldBeDisposable", Scope = "type", Target = "PSFilterLoad.PSApi.BufferProcs")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1049:TypesThatOwnNativeResourcesShouldBeDisposable", Scope = "type", Target = "PSFilterLoad.PSApi.ChannelPortProcs")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1049:TypesThatOwnNativeResourcesShouldBeDisposable", Scope = "type", Target = "PSFilterLoad.PSApi.FilterRecord")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1049:TypesThatOwnNativeResourcesShouldBeDisposable", Scope = "type", Target = "PSFilterLoad.PSApi.HandleProcs")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1049:TypesThatOwnNativeResourcesShouldBeDisposable", Scope = "type", Target = "PSFilterLoad.PSApi.PropertyProcs")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1049:TypesThatOwnNativeResourcesShouldBeDisposable", Scope = "type", Target = "PSFilterLoad.PSApi.ReadDescriptorProcs")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1049:TypesThatOwnNativeResourcesShouldBeDisposable", Scope = "type", Target = "PSFilterLoad.PSApi.ResourceProcs")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1049:TypesThatOwnNativeResourcesShouldBeDisposable", Scope = "type", Target = "PSFilterLoad.PSApi.WriteDescriptorProcs")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope = "member", Target = "PSFilterHostDll.PSFilterHost.#PseudoResources")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes", Scope = "member", Target = "PSFilterLoad.PSApi.RegionExtensions.#GetRegionScans(System.IntPtr,System.Drawing.Rectangle[]&)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes", Scope = "member", Target = "PSFilterHostDll.BGRASurface.BGRASurfaceMemory.#AllocateLarge(System.Int64)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes", Scope = "member", Target = "PSFilterHostDll.BGRASurface.BGRASurfaceMemory.#Allocate(System.UInt64)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Scope = "member", Target = "PSFilterLoad.PSApi.LoadPsFilter.#SetProgressFunc(PSFilterLoad.PSApi.ProgressProc)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Scope = "member", Target = "PSFilterLoad.PSApi.LoadPsFilter.#SetAbortFunc(PSFilterHostDll.AbortFunc)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Scope = "member", Target = "PSFilterLoad.PSApi.AETEEvent.#desc")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Scope = "member", Target = "PSFilterLoad.PSApi.AETEEvent.#enums")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Scope = "member", Target = "PSFilterLoad.PSApi.AETEEvent.#eventClass")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Scope = "member", Target = "PSFilterLoad.PSApi.AETEEvent.#flags")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Scope = "member", Target = "PSFilterLoad.PSApi.AETEEvent.#paramType")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Scope = "member", Target = "PSFilterLoad.PSApi.AETEEvent.#replyType")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Scope = "member", Target = "PSFilterLoad.PSApi.AETEEvent.#type")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Scope = "member", Target = "PSFilterLoad.PSApi.AETEEvent.#vendor")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Scope = "member", Target = "PSFilterHostDll.BGRASurface.ColorBgra16.#Bgra")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Scope = "member", Target = "PSFilterHostDll.BGRASurface.ColorBgra8.#Bgra")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes", Scope = "member", Target = "PSFilterLoad.PSApi.Memory.#Allocate(System.Int64,System.Boolean)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes", Scope = "member", Target = "PSFilterLoad.PSApi.Memory.#ReAlloc(System.IntPtr,System.Int64)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "srcColumnFloor", Scope = "member", Target = "PSFilterHostDll.BGRASurface.Surface16.#BicubicFitSurfaceChecked(PSFilterHostDll.BGRASurface.SurfaceBase,System.Drawing.Rectangle)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "srcColumnFloor", Scope = "member", Target = "PSFilterHostDll.BGRASurface.Surface16.#BicubicFitSurfaceUnchecked(PSFilterHostDll.BGRASurface.SurfaceBase,System.Drawing.Rectangle)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "srcColumnFloor", Scope = "member", Target = "PSFilterHostDll.BGRASurface.Surface32.#BicubicFitSurfaceChecked(PSFilterHostDll.BGRASurface.SurfaceBase,System.Drawing.Rectangle)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "srcColumnFloor", Scope = "member", Target = "PSFilterHostDll.BGRASurface.Surface32.#BicubicFitSurfaceUnchecked(PSFilterHostDll.BGRASurface.SurfaceBase,System.Drawing.Rectangle)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "srcColumnFloor", Scope = "member", Target = "PSFilterHostDll.BGRASurface.Surface64.#BicubicFitSurfaceChecked(PSFilterHostDll.BGRASurface.SurfaceBase,System.Drawing.Rectangle)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "srcColumnFloor", Scope = "member", Target = "PSFilterHostDll.BGRASurface.Surface64.#BicubicFitSurfaceUnchecked(PSFilterHostDll.BGRASurface.SurfaceBase,System.Drawing.Rectangle)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "srcColumnFloor", Scope = "member", Target = "PSFilterHostDll.BGRASurface.Surface8.#BicubicFitSurfaceChecked(PSFilterHostDll.BGRASurface.SurfaceBase,System.Drawing.Rectangle)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "srcColumnFloor", Scope = "member", Target = "PSFilterHostDll.BGRASurface.Surface8.#BicubicFitSurfaceUnchecked(PSFilterHostDll.BGRASurface.SurfaceBase,System.Drawing.Rectangle)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "PSFilterLoad.PSApi.LoadPsFilter.#RestoreParameters()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "source", Scope = "member", Target = "PSFilterLoad.PSApi.LoadPsFilter.#WriteBasePixels(System.IntPtr,PSFilterLoad.PSApi.VRect&,PSFilterLoad.PSApi.PixelMemoryDesc)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "source", Scope = "member", Target = "PSFilterLoad.PSApi.LoadPsFilter.#DisplayPixelsProc(PSFilterLoad.PSApi.PSPixelMap&,PSFilterLoad.PSApi.VRect&,System.Int32,System.Int32,System.IntPtr)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Scope = "member", Target = "PSFilterLoad.PSApi.LoadPsFilter.#AdvanceStateProc()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Scope = "member", Target = "PSFilterLoad.PSApi.LoadPsFilter.#DisplayPixelsProc(PSFilterLoad.PSApi.PSPixelMap&,PSFilterLoad.PSApi.VRect&,System.Int32,System.Int32,System.IntPtr)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Scope = "member", Target = "PSFilterLoad.PSApi.LoadPsFilter.#Dispose(System.Boolean)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Scope = "member", Target = "PSFilterLoad.PSApi.LoadPsFilter.#EnumPiPL(System.IntPtr,System.IntPtr,System.IntPtr,System.IntPtr)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Scope = "member", Target = "PSFilterLoad.PSApi.LoadPsFilter.#FillInputBuffer(System.IntPtr&,System.Int32&,PSFilterLoad.PSApi.Rect16,System.Int16,System.Int16,System.Int32,System.Int16)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Scope = "member", Target = "PSFilterLoad.PSApi.LoadPsFilter.#FillOutputBuffer(System.IntPtr&,System.Int32&,PSFilterLoad.PSApi.Rect16,System.Int16,System.Int16,System.Int16)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Scope = "member", Target = "PSFilterLoad.PSApi.LoadPsFilter.#GetErrorMessage(System.Int16)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Scope = "member", Target = "PSFilterLoad.PSApi.LoadPsFilter.#PropertyGetProc(System.UInt32,System.UInt32,System.Int32,System.IntPtr&,System.IntPtr&)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Scope = "member", Target = "PSFilterLoad.PSApi.LoadPsFilter.#ReadPixelsProc(System.IntPtr,PSFilterLoad.PSApi.PSScaling&,PSFilterLoad.PSApi.VRect&,PSFilterLoad.PSApi.PixelMemoryDesc&,PSFilterLoad.PSApi.VRect&)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Scope = "member", Target = "PSFilterLoad.PSApi.LoadPsFilter.#StoreOutputBuffer(System.IntPtr,System.Int32,PSFilterLoad.PSApi.Rect16,System.Int32,System.Int32)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Scope = "member", Target = "PSFilterHostDll.BGRASurface.SurfaceFactory.#CreateFromBitmapSource(System.Windows.Media.Imaging.BitmapSource,PSFilterLoad.PSApi.ImageModes&)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "PSFilterHostDll.BGRASurface.SurfaceFactory.#CreateFromBitmapSource(System.Windows.Media.Imaging.BitmapSource,PSFilterLoad.PSApi.ImageModes&)")]
