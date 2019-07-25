# NuGet package for EMDK Xamarin Component

###This application is provided without guarantee or warranty

##Purpose
This **proof of concept** shows how the [EMDK for Xamarin component provided by Zebra Technologies](https://developer.zebra.com/community/android/xamarin) can be converted to a NuGet package.

###Why do this?
Xamarin developers have two options to install 3rd party functionality into their applications:
- Xamarin Components which package both embedded and UI functionality into a neat bundle with associated documentation, samples and other helpful guides.  Xamarin components can optionally be monetized.
- NuGet which is the standard package manager for Microsoft technologies

With the acquisition of Xamarin by Microsoft there is essentially duplicated functionality here and many Xamarin component developers will choose to make their components available on both distribution platforms, especially free components.

##Build Instructions

The pre-built nupkg files are available as releases but you can follow these instructions to convert the dll to a binary
* Download nuget.exe from https://dist.nuget.org/win-x86-commandline/latest/nuget.exe.  
* Navigate to the same directory as the .nuspec from this repository.
* Optionally replace the `/lib/MonoAndroid/Symbol.XamarinEMDK.dll` with a different version (if you do this I also recommend updating the version in the .nuspec file to avoid confusion)
* With nuget.exe on your path run `nuget pack Symbol.XamarinEMDK.nuspec`

##Installation Instructions
To install the nupkg file the process is the same as any other local NuGet package.  Note that I have *not* uploaded this to https://www.nuget.org primarily because it is just a proof of concept though that could be done in the future.

In Visual Studio (2013 or 2015 for now as those are the only versions supported by EMDK for Xamarin 2.1.0.5): 
* Tools -> NuGet Package Manager --> Package Manager Settings.
* Select NuGet Package manager --> Package Sources
* Define a package source for the local nupkg file & OK out of that Window
* Tools --> NuGet Package manager --> Manager NuGet Packages for Solution
* Package source drop down: [Whatever you defined in the previous step]
* Install the package.

You now have access to the Symbol.XamarinEMDK namespace in your C# code, just as you would have done had you installed the official Xamarin component.

##Give it a Try

The most straight forward way I have found to test this is against the official EMDK for Xamarin samples: 
* Download the source code for a sample from http://techdocs.zebra.com/emdk-for-xamarin/latest/samples/
* Remove all references to the Symbol.Xamarin component and delete the .dll from the \lib folder
* Install the NuGet package as described above
* The sample should work as described in that sample's documentation
