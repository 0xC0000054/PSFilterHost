8bf filter host for .NET

Description
------------

This library allows .NET applications built with .NET 2.0 and later to run 3rd party 8bf filters.

Either 32-bit or 64-bit filters will be used depending on the processor architecture of the host process.

For 32-bit hosts the C# compiler enables Data Execution Prevention for the process, as many filters are not compatible with it you should use editbin or a similar tool to clear the IMAGE_DLLCHARACTERISTICS_NX_COMPAT flag
(see http://blogs.msdn.com/b/ed_maurer/archive/2007/12/14/nxcompat-and-the-c-compiler.aspx for more details).

For more details, or to report bugs, please refer to the website:
https://github.com/0xC0000054/PSFilterHost

Library Versions
-----------------

The .NET 2.0 version uses GDI+ (aka System.Drawing) it does not support processing 16-bits-per-channel images, and will convert the image data to the appropriate 8-bits-per-channel format.

The .NET 3.0, 3.5 and 4.5.2 versions use Windows Imaging Component (aka System.Windows.Media.Imaging) to support 8 and 16 bits per channel gray scale and RGB(A) images, the image data will be converted into the appropriate mode for processing.


Licensing 
----------

This software is licensed under the terms of the Microsoft Public License.
Please see License.txt for details.
