# 8bf filter host for .NET

 A library that enables applications built with .NET 2.0 or later to run 3rd party 8bf filters. 

## Features:

* Runs 32-bit or 64-bit filters based on the processor architecture of the host process.
*  Supports processing 8 and 16 bit per channel gray scale and RGB(A) images (16-bit processing is only supported with the BitmapSource class in .NET 3.0 or later). 
*  Exposes the image EXIF and XMP metadata to the filters.
*   Supports batch processing filters.

## Library Versions

The .NET 2.0 version uses GDIPlus (aka System.Drawing) it does not support 16-bit images, and converts all images to 32-bit BGRA internally.

The .NET 3.0, 3.5 and 4.5.2 versions use Windows Imaging Component (aka System.Windows.Media.Imaging) to support 8-bit and 16-bit Grayscale and RGBA images, the image data will be converted into the appropriate mode for processing.

## NuGet Package

https://www.nuget.org/packages/PSFilterHost/


## License

This project is licensed under the terms of the Microsoft Public License.   
See [License.txt](License.txt) for more information.