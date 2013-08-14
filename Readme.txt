Adobe(R) Photoshop(R) filter host for .NET

Description
------------

This library allows .NET applications built with .NET 2.0 and later to run 3rd party 8bf filters.

Either 32-bit or 64-bit filters will be used depending on the processor architecture of the host process.

For 32-bit hosts the C# compiler enables Data Execution Prevention for the process, as many filters are not compatible with it you should use editbin or a similar tool to clear the IMAGE_DLLCHARACTERISTICS_NX_COMPAT flag
(see http://blogs.msdn.com/b/ed_maurer/archive/2007/12/14/nxcompat-and-the-c-compiler.aspx for more details).

For more details, or to report bugs, please refer to the website:
http://psfilterhost.codeplex.com

Library Versions
-----------------

The .NET 2.0 version uses GDIPlus (aka System.Drawing) it does not support 16-bit images, and converts all images to 32-bit BGRA internally.

The .NET 3.0, 3.5 and 4.0 versions use Windows Imaging Component (aka System.Windows.Media.Imaging) to support 8-bit and 16-bit Grayscale and RGBA images, the image data will be converted into the appropriate mode for processing.


Licensing 
----------

This software is open-source under the Microsoft Public License.
Please see License.txt for details.
