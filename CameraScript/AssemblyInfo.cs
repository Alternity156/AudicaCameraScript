using System.Resources;
using System.Reflection;
using System.Runtime.InteropServices;
using MelonLoader;

[assembly: AssemblyTitle(CameraScript.BuildInfo.Name)]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany(CameraScript.BuildInfo.Company)]
[assembly: AssemblyProduct(CameraScript.BuildInfo.Name)]
[assembly: AssemblyCopyright("Created by " + CameraScript.BuildInfo.Author)]
[assembly: AssemblyTrademark(CameraScript.BuildInfo.Company)]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
//[assembly: Guid("")]
[assembly: AssemblyVersion(CameraScript.BuildInfo.Version)]
[assembly: AssemblyFileVersion(CameraScript.BuildInfo.Version)]
[assembly: NeutralResourcesLanguage("en")]
[assembly: MelonModInfo(typeof(CameraScript.CameraScript), CameraScript.BuildInfo.Name, CameraScript.BuildInfo.Version, CameraScript.BuildInfo.Author, CameraScript.BuildInfo.DownloadLink)]


// Create and Setup a MelonModGame to mark a Mod as Universal or Compatible with specific Games.
// If no MelonModGameAttribute is found or any of the Values for any MelonModGame on the Mod is null or empty it will be assumed the Mod is Universal.
// Values for MelonModGame can be found in the Game's app.info file or printed at the top of every log directly beneath the Unity version.
[assembly: MelonModGame(null, null)]