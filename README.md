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

SatsuiMemory is separated in two parts (in fact three, but we will see this later).  
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
	myPatch.Info.Compatibility = Platforms.x86; // 32 bits
	myPatch.Info.Compatibility = Platforms.x64; // 64 bits

## Shared variables  ##

A patch contains a list of variables.  
They are shared between your application and the remote process. 

	PatchDataVar var = new PatchDataVar (
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

If the platform is not specified, variables will be loaded in 32 bits and 64 bits applications.  
Except if you targeted the platform in the **Info.Compatibility** of your patch.  
In the **DemoConsole** project, i created two variables.  
I do not need to specify a platform because i targeted it in the patch info :

	myPatch.Info.Compatibility = Platforms.x86;
	myPatch.Data.Vars.Add(new PatchDataVar(VarType.Numeric, "MyFlagCounter"));
    myPatch.Data.Vars.Add(new PatchDataVar(VarType.Numeric, "Flags", "Winmine__XP.exe+5194", null));

The second important thing to know is the **expression** parameter.  
If you leave it empty, like i done for the **MyFlagCounter** variable, SatsuiMemory will allocate space in memory of the targeted process.  
But if you set an expression value, like i done for the **Flags** variable, SatsuiMemory will read and write the memory at this place.  
In a variable expression, you can use **module** name, **hexadecimal** value, and your **own variables** !

	// Will read and write at the offset 0x5000 (hexadecimal)
	myPatch.Data.Vars.Add(new PatchDataVar(VarType.Numeric, "TestVar1", "5000", null));

	// Will read and write at 'Winmine__XP.exe' module address
	myPatch.Data.Vars.Add(new PatchDataVar(VarType.Numeric, "TestVar2", "Winmine__XP.exe", null));
	
	// Will convert the value of $TestVar2 to an offset and will read/write to it
	myPatch.Data.Vars.Add(new PatchDataVar(VarType.Numeric, "TestVar3", "$TestVar2", null));
	
You can concatenate elements in an expression :

	myPatch.Data.Vars.Add(new PatchDataVar(VarType.Numeric, "TestVar4", "Winmine__XP.exe+$TestVar1+1000", null));

## Hooks ##

In the **DemoConsole** project, for the Freeze timer function, i could have created a timer to set the variable **Flags** every seconds.  
But instead, i created a hook to remove the code which decrease the flags count.  
First you need to find the piece of code (using CheatEngine for example) doing it. Then, you replace it with your own function.  
In the demo project the original code look like this :

	8B 44 24 04 // move eax, [esp+04]
	01 05 94 51 00 01 // add [01005194], eax

This is the **AOB** of the patch.  
Now, you need to write the code which will replace the AOB.  
In the **DemoConsole** project, i decited to increment a shared variable. But in fact, you can choose to do nothing :

	FF 05 [MyFlagCounter] // inc [MyFlagCounter]

SatsuiMemory will replace **[MyFlagCounter]** with the right memory address because we declared it above as a shared variable.  
The complete code look like this :

	PatchHook hook = new PatchHook(
		"Unlimited flags", // Name of the hook
		"8B 44 24 04 01 05 94 51 00 01", // AOB
		"FF 05 [MyFlagCounter]"); // My replacement code
	myPatch.Hooks.Add(hook);

This case is simple because the AOB never change, its always **move eax, [esp+04]**.  
But imagine if the number 04 change everytime we start the game.  
SatsuiMemory will not found the AOB and the hook will be rejected.  
In this case we can replace unknown bytes by a **question mark** : 

	8B 44 24 ?? // move eax, [esp+??]

Okay, but i need to execute the original code before my own code, how to do it ?  
You can use a special function in your remplacement code : **[aob:** (start) **:** (length) **]**  
For example :

	8B 44 24 [aob:3:1] // move eax, [esp+??]
	

## Saving and loading a patch ##

Now you created and tested the patch, you can save and load it from a file using a **serializer**.  
You can choose to serialize in two formats : **Xml** and **Binary** :

	using SatsuiMemory.Serialization;
	Serializer.SetDefaultFormat(SerializeFormat.Binary);
 
Saving your patch :

	myPatch.Save("my-patch-file." + Serializer.DefaultFileExtension);

Loading your patch :

	Patch myPatch = Patch.FromFile("my-patch-file." + Serializer.DefaultFileExtension);



# Well, i created a patch, how to use it now ? #

Well, now you done the 'hard work', you juste have to exploit it with a **MemApp** :  

	MemApp myApp = new MemApp(myPatch);
	myApp.OpenSync();
	if (myApp.State == MemAppState.Opened)
	{
		// Play with memory :P
		// Don't forget to close it after !
		myApp.Close();
	}
	
## Accessing and editing shared variables ##

To access a shared variable, you juste have to use the function **GetVar**, givint it the name of the variable :

	MemDataVar myVar = myApp.Data.GetVar("Flags");
	
	int flagsCounter = Convert.ToInt32(myVar.Value); // Get the flags counter
	myVar.Value = 999; // Set the flags counter to 999


## Enabling and disabling hooks ##

To enable a hook, you can use the property **IsEnabled** :

	myApp.Hooks[0].IsEnabled = true; // Enable the first hook
	myApp.Hooks[0].IsEnabled = false; // Disable the first hook

## Closing the MemApp ##

When you finished to play with the memory, don't forget to close the MemApp.  
It will restore the targeted process as his original state.

	myApp.Close(); // Important !

## Memory cache ##

For **huge programs**, it can take few moments for SatsuiMemory to find AOBs.  
But you can use the cache system to do it faster.  
You need to load the cache before opening the MemApp :  

	MemApp myApp = new MemApp(myPatch);
	myApp.Cache.Load("my-cache-file." + Serializer.DefaultFileExtension);

Then before to close the MemApp, you can save the cache :
	
	myApp.Cache.Save("my-cache-file." + Serializer.DefaultFileExtension);
	myApp.Close();
	

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

