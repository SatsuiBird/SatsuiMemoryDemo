**[ ! ] This description is in progress, come back later to get all informations [ ! ]**

<p align="center">
  <img src="http://github.messatsu-dojo.com/previews/satsuimemory-1.png" alt="SatsuiMemory preview"/>
</p>

# What is SatsuiMemory ? #

SatsuiMemory is a library allowing you to view and edit applications memory.  
It's easy, fast to use and free.

# How to install it ? #

The Nuget package is available here : [https://www.nuget.org/packages/Messatsu.SatsuiMemory/](https://www.nuget.org/packages/Messatsu.SatsuiMemory/ "Messatsu.SatsuiMemory")

    PM> Install-Package Messatsu.SatsuiMemory

# How to use it ? #

SatsuiMemory is separated in two parts (in fact three, but we will se this later).  
First of all, you create a **Patch**.
It contains all informations about memory operations, but do nothing.  
Then you create a **MemApp** which will use the Patch created before and execute all instructions.  
So you can serialize and **distribute** your Patch as a file or though a repository (this is the third part).


# How to create a Patch ? #

Creating a patch is the 'hard work' of the goal.  
SatsuiMemory will not do the work at your place but will help you to apply it.  
So you still need to have a little knowledge in reverse engineering to create a patch.

First of all, include the SatsuiMemory namespaces :

	using SatsuiMemory;
	using SatsuiMemory.Patching;
	using SatsuiMemory.Memory;

Then instantiate a Patch object and give him the process targeted **without** .exe at the end.  
If needed, you can specify the window title

	Patch myPatch = new Patch("name_of_the_process");
	Patch myPatch = new Patch("name_of_the_process", "window title");

We will see below how to create a patch compatible with **x86** (32 bits) and **x64** (64 bits) platforms.  
But if you want to be sure to target only one platform, you can set the compatibility :

	myPatch.Info.Compatibility = Platforms.All; // All platforms (set by default)
	myPatch.Info.Compatibility = Platforms.x86; // 32bits
	myPatch.Info.Compatibility = Platforms.x64; // 64bits

## Shared variables  ##

A patch contains a list of variables.  
They are shared between your application and the remote process. 

	PatchDataVar var = new PatchDataVar(
		Platforms platforms (default = Platforms.All), 
		VarType type, 
		string name, 
		int size (default = 0), 
		string expression (default = ""), 
		object defaultValue (default = null)
	);

The first parameter is the targeted platform.  
So if your patch is set for **Platforms.All**, you can decide which variables are loaded in the current process.  
For example :

	// Will be loaded in 32 bits apps only
	myPatch.Data.Vars.Add(new PatchDataVar(Platforms.x86, VarType.Numeric, "TestVar1")); 

	// Will be loaded in 64 bits apps only
	myPatch.Data.Vars.Add(new PatchDataVar(Platforms.x64, VarType.Numeric, "TestVar2")); 

If the platform is not specified, variables will be loaded in 32bits and 64bits applications.  
Except if you targeted the platform in the **Info.Compatibility** of your patch.  
In the **DemoConsole** project, i created two variables.  
I do not need to specify a platform because i targeted it in the patch info :

	myPatch.Info.Compatibility = Platforms.x86;
	myPatch.Data.Vars.Add(new PatchDataVar(VarType.Numeric, "MyFlagCounter"));
    myPatch.Data.Vars.Add(new PatchDataVar(VarType.Numeric, "Flags", "Winmine__XP.exe+5194", null));

The second important thing to know is the **expression** parameter.  
If you leave it empty, like i done for my **MyFlagCounter** variable, SatsuiMemory will allocate space in the memory of the targeted process.  
But if you set an expression value, like i done for the **Flags** variable, SatsuiMemory will read and write the memory at this place.  
In a variable expression, you can use **module** name, **hexadecimal** value, and your **own variables** !

	// Will read and write at the offset 0x5000 (hexadecimal)
	myPatch.Data.Vars.Add(new PatchDataVar(VarType.Numeric, "TestVar1", "5000", null));

	// Will read and write at 'Winmine__XP.exe' module address
	myPatch.Data.Vars.Add(new PatchDataVar(VarType.Numeric, "TestVar2", "Winmine__XP.exe", null));
	
	// Will read and write at $TestVar2 offset value
	myPatch.Data.Vars.Add(new PatchDataVar(VarType.Numeric, "TestVar3", "$TestVar2", null));
	
You can concatenate elements in a expression :

	myPatch.Data.Vars.Add(new PatchDataVar(VarType.Numeric, "TestVar4", "Winmine__XP.exe+$TestVar1+1000", null));

## Hooks ##

*Coming soon.  
The description is still in progress !*

## Saving and loading a patch ##

*Coming soon.  
The description is still in progress !*

# I created a patch, how to use it now ? #

Well, now you done the 'hard work', you juste have to use it !  

	MemApp myApp = new MemApp(myPatch);
	myApp.OpenSync();
	if (myApp.State == MemAppState.Opened)
	{
		// Play with memory :P
	}
	
## Accessing and editing variables ##

*Coming soon.  
The description is still in progress !*

## Enabling and disabling hooks ##

*Coming soon.  
The description is still in progress !*


# My others projects #

**SatsuiUi**  
Set of controls and skins for WPF applications  
[https://github.com/SatsuiBird/SatsuiUiDemo](https://github.com/SatsuiBird/SatsuiUiDemo "SatsuiUiDemo")

**SatsuiLocalization**  
Easy localization of .NET applications  
[https://github.com/SatsuiBird/SatsuiLocalizationDemo](https://github.com/SatsuiBird/SatsuiLocalizationDemo "SatsuiLocalizationDemo")

**SatsuiOverlay**  
Create customizable HTTP widgets  
[https://github.com/SatsuiBird/SatsuiOverlayDemo](https://github.com/SatsuiBird/SatsuiOverlayDemo "SatsuiOverlayDemo")

