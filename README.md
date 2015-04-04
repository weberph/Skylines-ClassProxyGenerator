# Skylines-ClassProxyGenerator
Creates proxy classes to support updating of mods at runtime that register types with `AddComponent` or `AddUIComponent`.

Writing Cities: Skylines mods is fun until you have to restart the game since the calls to `AddComponent` or `AddUIComponent` do not recognize the updated version of your mod (e.g. you get an `InvalidCastException` while casting the result of `AddComponent` or `AddUIComponent` to your class or your changes are simply not applied).

Skylines-ClassProxyGenerator is a command line utility that can create proxy classes for types that are passed to `AddComponent` or `AddUIComponent` which creates a unique identity for each type passed to these functions.
Although it is a bit hacky, it forces the game to always use the most updated version of a class and accelerates debugging.

---

### Usage

In order to use this tool, you need to modify your mod:

1. Provide a public class called `Mod` with a public static getter of type `Type[]` that returns the types for which proxy classes should be generated.

   Example:
   ```csharp
public class Mod
{
        public static Type[] ProxyTypes
        {
            get
            {
                return new[] { typeof(MyPanel) };
            }
        }
        // ...
}
   ```
2. Add logic to load the proxy (see SampleMod)

   Example:
  - include `ProxyLoader.cs`
  - add helper method to class `Mod`:
   ```csharp
public class Mod
{
        // ...
        private static ProxyLoader _proxyLoader = null;
    
        public static Type GetProxy<T>()
        {
            _proxyLoader = _proxyLoader ?? new ProxyLoader(@"<path to your proxy dll>");
            return _proxyLoader.GetProxy<T>();
        }
}
   ```
   *Note: the proxy dll is generated during step 5*
   
   **_Warning:_** _do **not** place the proxy dll in the mod directory of Cities: Skylines_

3. Use the proxy classes for calls to `AddComponent` or `AddUIComponent`

  Example:
   ```csharp
var gameObject = new GameObject(PanelGameObjectName);
var myPanel = (MyPanel)gameObject.AddComponent(Mod.GetProxy<MyPanel>());
   ```
   
4. Compile your mod

5. Run the ClassProxyGenerator command line tool

   Example:
  - `ClassProxyGenerator.exe c:\MyMod\MyMod.dll c:\Steam\SteamApps\common\Cities_Skylines\Cities_Data\Managed\`
    
    This will create "MyModProxy.dll" in "c:\MyMod\"

6. Deploy your mod

---

Step 4-6 can be automated with a post build step:
```
mkdir "%LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods\$(SolutionName)"
del "%LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods\$(SolutionName)\$(TargetFileName)"
ClassProxyGenerator.exe c:\MyMod\MyMod.dll c:\Steam\SteamApps\common\Cities_Skylines\Cities_Data\Managed\
xcopy /y "$(TargetPath)" "%LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods\$(SolutionName)"
```
