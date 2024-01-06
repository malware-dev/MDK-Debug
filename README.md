# MDK-Debug
*** NON-FUNCTIONAL ***
Since Keen now has removed direct plugin support, this project is dead. It would have to be rewritten to be a PluginLoader plugin - but since I personally don't use this plugin any longer, I have no incentive to maintain it. Sorry.

Utility plugin for Space Engineers, allowing direct debugging of Programmable Block scripts via Space Engineers. Designed for MDK projects.

**This plugin owes its existence to Inflex, who created the first version of it. By his permission I have rewritten and adapted it for use with MDK projects, but his basic methodology survives.**

## Remarks
This plugin is currently a prototype. It should _work_ but it's technically not in a "releasable" state yet and I haven't really decided how much I should truly integrate it with MDK. Most likely the assembly selection method will remain
for people who don't want to use MDK, and we'll see if I integrate it more completely with MDK.


## Usage
* Installation:
  Create a shortcut on your desktop, or start menu or whereever you want it.  
  Set it up to point to your Steam.exe file (you'll probably find it in `"C:\Program Files (x86)\Steam"`).  
  Configure its arguments so they look like this: `Steam.exe -applaunch 244850 -plugin "path\to\MDK-Debug.dll"
* Debugging: 
  * Start the debugging shortcut
    * It is **highly recommended** that you set your Space Engineers up to run in a borderless window or windowed mode, not fullscreen. Otherwise you'll likely get Space Engineers on top, unresponsive, with Visual Studio on a breakpoint behind it, being difficult to get at.
  * Open the MDK project you wish to debug
  * Compile it
  * Make sure SE has completely started, then - in Visual Studio - press the `Debug` menu, `Attach to Process`, and select Space Engineers in the process list.
  * In Space Engineers, Create or load a world where you have a programmable block you wish to test in
  * Open the programmable block in question, select "MDK-SE: Bind DLL"
  * Select the `.exe` file generated for your MDK project. You'll find it in the `bin` folder of your MDK project
    * The program will be immediately loaded and started, so you might want to have placed breakpoints already.
    
    
