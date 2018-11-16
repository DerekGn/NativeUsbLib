# NativeUsbLib

This is a port of the excellent .net Usb Library from the sourceforge project [
UsbViewer in C#](https://sourceforge.net/projects/usbviewerincsha/) by [nick vetter](https://sourceforge.net/u/nickvetter/profile/). The library has not been updated for quite some time and is targeting .net framework 2.0. There are also issues running on x64 builds of Windows. This repo updates the runtimes and resolves the x64 runtime issues. The library is also available on [nuget.org](https://www.nuget.org/packages/NativeUsbLib).

## Installing NativeUsbLib

Install the NativeUsbLib package via nuget package manager console:

```powershell
Install-Package NativeUsbLib
```

## Supported .Net Runtimes

* .net 2.0
* .net 4.5
* .net 4.6

## Supported Operating Systems

* Windows 7 x86/x64
* Windows 8 x86/x64
* Windows 10 x86/x64