# 8bf filter host for .NET
 
 [![NuGet version](https://img.shields.io/nuget/v/PSFilterHost.svg?style=flat)](https://www.nuget.org/packages/PSFilterHost/)

 A library that enables applications built with .NET 2.0 or later to run 3rd party 8bf filters. 

## Features:

* Runs 32-bit or 64-bit filters based on the processor architecture of the host process.
*  Supports processing 8 and 16 bits per channel gray scale and RGB(A) images (16-bit processing is only supported with the BitmapSource class in .NET 3.0 or later). 
*  Exposes the image EXIF and XMP metadata to the filters.
*   Supports batch processing filters.

## Library Versions

The .NET 2.0 version uses GDIPlus (aka System.Drawing) it does not support processing 16-bits-per-channel images, and will convert the image data to the appropriate 8-bits-per-channel format.

The .NET 3.0, 3.5 and 4.5.2 versions use Windows Imaging Component (aka System.Windows.Media.Imaging) to support 8-bit and 16-bit Grayscale and RGBA images, the image data will be converted into the appropriate mode for processing.

## License

This project is licensed under the terms of the Microsoft Public License.   
See [License.txt](License.txt) for more information.