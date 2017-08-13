using System;
using System.Diagnostics;
using System.IO;
using SatsuiMemory;
using SatsuiMemory.Patching;
using SatsuiMemory.Serialization;
using SatsuiMemory.Memory;

namespace DemoConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            // Starting the game
            Console.WriteLine("Starting the game...");
            if(StartGame())
            {
                // You can choose to serialize as binary format if you want
                //Serializer.SetDefaultFormat(SerializeFormat.Binary);

                Console.WriteLine("------------------------------");

                // This patch is for the game Minesweeper (Winmine__XP.exe)
                Patch myPatch = new Patch("Winmine__XP");

                // Its a 32 bits application
                myPatch.Info.Compatibility = Platforms.x86;

                // Data contains all variables that you can use in hooks code
                // Its a sort of shared memory
                // See https://github.com/SatsuiBird/SatsuiMemoryDemo to get more informations about shared variables
                myPatch.Data.Vars.Add(new PatchDataVar(VarType.Numeric, "MyFlagCounter"));
                myPatch.Data.Vars.Add(new PatchDataVar(VarType.Numeric, "Flags", "Winmine__XP.exe+5194", null));

                // Creating the hook for unlimited flags
                string aob = "", code = "";
                aob = "8B 44 24 04"; // move eax, [esp+04]
                aob += "01 05 94 51 00 01"; // add [01005194], eax

                // The code will increment our MyFlagCounter everytime the game try to decrements flags count
                code = "FF 05 [MyFlagCounter]"; // inc [MyFlagCounter]
                myPatch.Hooks.Add(new PatchHook("Unlimited flags", aob, code));

                // Creating the hook to freeze time
                aob = "FF 05 9C570001"; // inc [0100579C]
                code = ""; // No code needed, we just remove the incrementation
                myPatch.Hooks.Add(new PatchHook("Freeze timer", aob, code));

                // When the patch is created, you can save it to a file
                myPatch.Save(AppDomain.CurrentDomain.BaseDirectory + @"patchs\minesweeper." + Serializer.DefaultFileExtension);

                // Then, load it later...
                //patch = Patch.FromFile(AppDomain.CurrentDomain.BaseDirectory + @"patchs\minesweeper." + Serializer.DefaultFileExtension);

                // Creating the memory application using the patch
                MemApp myApp = new MemApp(myPatch);
                myApp.DataVarInstalled += App_DataVarInstalled;
                myApp.HookInstalled += App_HookInstalled;

                // Setting the cache to resolve faster memory addresses
                // You can load it from a file
                if (!myApp.Cache.Load(AppDomain.CurrentDomain.BaseDirectory + @"cache\" + myPatch.ProcessName + "." + Serializer.DefaultFileExtension))
                {
                    // Unable to find the file, so you can add cache entries manually
                    // If the AOB don't correspond to the address, the cache will scan memory to find it and update the cache
                    // If you let the cache empty, the memory will be scanned to find each AOB
                    //app.Cache.Entries.Add(new MemCacheEntry(patch.Hooks[0].AOB, 0x0042391E)); // Step 2
                    //app.Cache.Entries.Add(new MemCacheEntry(patch.Hooks[1].AOB, 0x00423DC6)); // Step 3
                }

                // Opening the application memory synchronously
                myApp.OpenSync();

                // Finally, playing with the memory :P
                if (myApp.State == MemAppState.Opened)
                {
                    bool end = false;

                    Console.WriteLine("------------------------------");
                    Console.WriteLine("1 : Unlimited flags (enable/disable)");
                    Console.WriteLine("2 : Freezed timer (enable/disable)");
                    Console.WriteLine("3 : Set current flags to 999");
                    Console.WriteLine("4 : Read current flags");
                    Console.WriteLine("5 : Read MyFlagCounter");
                    Console.WriteLine("ESC : Exit");
                    Console.WriteLine("------------------------------");

                    while (!end)
                    {
                        if (myApp.Process.HasExited) break;

                        ConsoleKeyInfo key = Console.ReadKey(true);
                        switch (key.Key)
                        {
                            case ConsoleKey.NumPad1:
                                myApp.Hooks[0].IsEnabled = !myApp.Hooks[0].IsEnabled;
                                Console.WriteLine("--> Unlimited flags enabled = " + myApp.Hooks[0].IsEnabled);
                                break;

                            case ConsoleKey.NumPad2:
                                myApp.Hooks[1].IsEnabled = !myApp.Hooks[1].IsEnabled;
                                Console.WriteLine("--> Freezed timer enabled = " + myApp.Hooks[1].IsEnabled);
                                break;

                            case ConsoleKey.NumPad3:
                                myApp.Data.GetVar("Flags").Value = 999;
                                Console.WriteLine("--> Current flags set to 999");
                                break;

                            case ConsoleKey.NumPad4:
                                int flags = Convert.ToInt32(myApp.Data.GetVar("Flags").Value);
                                Console.WriteLine("--> Current flags : " + flags);
                                break;

                            case ConsoleKey.NumPad5:
                                int flagCounter = Convert.ToInt32(myApp.Data.GetVar("MyFlagCounter").Value);
                                Console.WriteLine("--> MyFlagCounter = " + flagCounter);
                                break;

                            case ConsoleKey.Escape:
                                end = true;
                                break;

                            default:
                                Console.WriteLine("Invalid choice");
                                break;
                        }
                    }

                    // Saving the cache for future memory access
                    // It will store correspondence between AOB and addresses
                    myApp.Cache.Save(AppDomain.CurrentDomain.BaseDirectory + @"cache\" + myPatch.ProcessName + "." + Serializer.DefaultFileExtension);

                    // Closing the application
                    // And freeing all allocated resources
                    // Be sure to do it, to restore the targeted application memory
                    myApp.Close();
                }
            }
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }


        #region Memory application events

        private static void App_HookInstalled(MemApp sender, MemHook hook)
        {
            Console.WriteLine("- Hook " + hook.Name + " found at 0x{0:X} and installed at 0x{1:X}", hook.HookAddress, hook.CodeAddress);
        }

        private static void App_DataVarInstalled(MemApp sender, MemDataVar var)
        {
            Console.WriteLine("- Var " + var.Name + " installed at 0x{0:X}", var.Address);
        }

        #endregion


        #region StartGame 

        /// <summary>
        /// Start the game if not already running
        /// </summary>
        /// 
        static bool StartGame()
        {
            string game = "Winmine__XP";
            foreach (Process process in Process.GetProcesses())
            {
               if(process.ProcessName.ToLower().Contains(game.ToLower()))
                {
                    Console.WriteLine("Game is already running !");
                    return true;
                }
            }

            try
            {
                string currentPath = AppDomain.CurrentDomain.BaseDirectory;
                Debug.WriteLine(currentPath);

                string gamePath = Path.GetFullPath(Path.Combine(currentPath, @"..\..\..\Games\Winmine__XP.exe"));
                if (!File.Exists(gamePath)) throw new FileNotFoundException("Unable to find the game at " + gamePath);

                Process.Start(gamePath);
                Console.WriteLine("Game started !");
                return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        #endregion


    }
}
