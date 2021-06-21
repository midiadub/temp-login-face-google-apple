using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
#if UNITY_IOS
using AppleAuth.Editor;
using UnityEditor.iOS.Xcode;
#endif

public static class Builder
{
    private static readonly string _buildDir = "Builds/";
    private static readonly string _assetBundledDir = "AssetBundles/";
    private static string _splashSceneFilePath = "Assets/StaticAssets/Scene/Splash.unity";


    private static string AndroidFileName => Application.productName + ".apk";

    private static string[] CheckFolders(string platform, bool delete = true)
    {
        if (!Directory.Exists(_buildDir))
        {
            Directory.CreateDirectory(_buildDir);
        }

        if (delete && Directory.Exists(_buildDir + platform))
            Directory.Delete(_buildDir + platform, true);

        return EditorBuildSettings.scenes.Select(x => x.path).ToArray();
    }

    static void Refresh()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
    }

    static string ReadArguments(string arg = "buildVersion")
    {
        string[] args = System.Environment.GetCommandLineArgs();
        string input = "";
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-" + arg)
            {
                input = args[i + 1];
            }
        }

        return input;
    }

    #region Build

    [MenuItem("Automation/iOS/Build")]
    public static void BuildIos()
    {
        Build(CheckFolders("iOS"), BuildTarget.iOS, "iOS",
            BuildTargetGroup.iOS, "", 2);
    }

    [MenuItem("Automation/iOS/Dev Build")]
    public static void BuildIosDev()
    {
        Build(CheckFolders("iOS"), BuildTarget.iOS, "iOS",
            BuildTargetGroup.iOS, "DEV_MODE", 2, true, true);
    }

    [MenuItem("Automation/Android/Build AAB")]
    public static void BuildAndroid()
    {
        Build(CheckFolders("Android"), BuildTarget.Android, AndroidFileName,
            BuildTargetGroup.Android, "", 2, true, false);
    }

    [MenuItem("Automation/Android/Build Apk")]
    public static void BuildAndroidApk()
    {
        Build(CheckFolders("Android"), BuildTarget.Android, AndroidFileName,
            BuildTargetGroup.Android, "", 2, false, false);
    }


    [MenuItem("Automation/Android/Dev Build %&C")]
    public static void BuildAndroidDev()
    {
        Build(CheckFolders("Android"), BuildTarget.Android, AndroidFileName,
            BuildTargetGroup.Android, "DEV_MODE", 2, false, true, true);
    }

    [MenuItem("Automation/Android/Dev Build IL2Cpp")]
    public static void BuildAndroidDevIL()
    {
        Build(CheckFolders("Android"), BuildTarget.Android, AndroidFileName,
            BuildTargetGroup.Android, "DEV_MODE", 2, true, true, false);
    }

    static void Build(string[] scenes, BuildTarget target, string name, BuildTargetGroup group, string symbols,
        int qualityLevel, bool il2Cpp = true, bool dev = false, bool devbuild = false)
    {
        //var backup = SetupAppVersionObject();

        AssetDatabase.Refresh();
        //EditorSceneManager.OpenScene(_splashSceneFilePath);

        try
        {
            QualitySettings.SetQualityLevel(qualityLevel);

            if (group == BuildTargetGroup.Android)
            {
                if (!il2Cpp)
                {
                    PlayerSettings.SetScriptingBackend(group, ScriptingImplementation.Mono2x);
                    PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
                }
                else
                {
                    PlayerSettings.SetScriptingBackend(group, ScriptingImplementation.IL2CPP);
                    AndroidArchitecture aac = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
                    PlayerSettings.Android.targetArchitectures = aac;
                }
            }
            else
            {
                if (!il2Cpp)
                {
                    PlayerSettings.SetScriptingBackend(group, ScriptingImplementation.Mono2x);
                }
                else
                {
                    PlayerSettings.SetScriptingBackend(group, ScriptingImplementation.IL2CPP);
                }
            }

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = scenes;
            buildPlayerOptions.locationPathName = _buildDir + target + "/" + name;
            buildPlayerOptions.target = target;
            buildPlayerOptions.targetGroup = group;

            buildPlayerOptions.options = devbuild ? BuildOptions.Development : BuildOptions.None;

            if (il2Cpp && target == BuildTarget.Android)
                EditorUserBuildSettings.buildAppBundle = true;
            else //if (target == BuildTarget.Android)
                EditorUserBuildSettings.buildAppBundle = false;

            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, $"{symbols}");

            SetSignPassword();

            BuildPipeline.BuildPlayer(buildPlayerOptions);

            if (!il2Cpp && group == BuildTargetGroup.Android)
                Execute(target, PlayerSettings.applicationIdentifier, name);
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
            //throw;
        }

        //RevertAppVersionObject(backup);

        AssetDatabase.Refresh();
        //EditorSceneManager.OpenScene(_splashSceneFilePath);
    }

    #endregion

    [MenuItem("Automation/Android/InstanAndRunLastBuild")]
    public static void InstallAndRunLastBuild()
    {
        Execute(BuildTarget.Android, PlayerSettings.applicationIdentifier, AndroidFileName);
    }

    static void Execute(BuildTarget target, string package, string name)
    {
        System.Diagnostics.Process process = new System.Diagnostics.Process();
        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
        //startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
        startInfo.FileName = "cmd.exe";

        var strCmdText =
            "/C "
            //+ "adb uninstall " + package
            + " adb install -r \"" + _buildDir + target + "/" + name + "\""
            + " && adb shell am start -S -a android.intent.action.MAIN -n " + package +
            //"/com.unity3d.player.UnityPlayerActivity"
            "/com.google.firebase.MessagingUnityPlayerActivity"
#if DEV_MODE
            + " && adb shell setprop debug.firebase.analytics.app" + package;
#else
            + " && adb shell setprop debug.firebase.analytics.app .none";
#endif

        startInfo.Arguments = strCmdText;
        process.StartInfo = startInfo;
        process.Start();
    }

    [MenuItem("Automation/Android/DeepLink/Signup")]
    public static void DeepLink()
    {
        System.Diagnostics.Process process = new System.Diagnostics.Process();
        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
        //startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
        startInfo.FileName = "cmd.exe";

        var strCmdText =
            "/C "
            //+ "adb uninstall " + package
            + " adb shell am start -a android.intent.action.VIEW -d \"budxnba://bud/signup\" com.midiadub.budxnba";

        startInfo.Arguments = strCmdText;
        process.StartInfo = startInfo;
        process.Start();
    }

    public static void SetSignPassword()
    {
        PlayerSettings.Android.keystorePass = "resolume";
        PlayerSettings.Android.keyaliasPass = "resolume";
    }

#if UNITY_IOS
    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string pathToBuildProject)
    {
        Debug.Log("Running PostProcessBuild Command (" + buildTarget + ")");

        if (buildTarget == BuildTarget.iOS)
        {

            try
            {
                var projectPath = PBXProject.GetPBXProjectPath(pathToBuildProject);
#if UNITY_2019_3_OR_NEWER
                    var project = new PBXProject();
                    project.ReadFromString(System.IO.File.ReadAllText(projectPath));
                    var manager = new ProjectCapabilityManager(projectPath, "Entitlements.entitlements", null, project.GetUnityMainTargetGuid());
                    manager.AddSignInWithAppleWithCompatibility(project.GetUnityFrameworkTargetGuid());
                    manager.WriteToFile();
#else
                    var manager = new ProjectCapabilityManager(projectPath, "Entitlements.entitlements", PBXProject.GetUnityTargetName());
                    manager.AddSignInWithAppleWithCompatibility();
                    manager.WriteToFile();
#endif
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
                //throw;
            }
            try
            {
                Debug.Log("PostProcessBuild CopyExportOptions");
                CopyExportOptions(pathToBuildProject);
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
            
            try
            {
                Debug.Log("NSUserTrackingUsageDescription Info Plist");
                // Get plist
                string plistPath = pathToBuildProject + "/Info.plist";
                PlistDocument plist = new PlistDocument();
                plist.ReadFromString(File.ReadAllText(plistPath));
       
                // Get root
                PlistElementDict rootDict = plist.root;
       
                // Change value of CFBundleVersion in Xcode plist
                var photoLibraryUsage = "NSUserTrackingUsageDescription";
                rootDict.SetString(photoLibraryUsage,"Your data will be used to provide you a better user experience.");//TODO: Define text
       
                // Write to file
                File.WriteAllText(plistPath, plist.WriteToString());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
            try
            {
                Debug.Log("PostProcessBuild ENABLE_BITCODE");
                string projectPath = pathToBuildProject + "/Unity-iPhone.xcodeproj/project.pbxproj";

                PBXProject pbxProject = new PBXProject();
                pbxProject.ReadFromFile(projectPath);

                string target = pbxProject.TargetGuidByName("Unity-iPhone");
                pbxProject.SetBuildProperty(target, "ENABLE_BITCODE", "NO");

                pbxProject.WriteToFile(projectPath);
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }

            //addPushAndAssociatedDomainCapability(pbxProject, pathToBuildProject);

            try
            {
                Debug.Log("PostProcessBuild AddAssociatedDomains");
                string projectPath = pathToBuildProject + "/Unity-iPhone.xcodeproj/project.pbxproj";
                string targetName = "Unity-iPhone";
                string entitlementsFileName = "budxnba.entitlements";

                var entitlements = new ProjectCapabilityManager(projectPath, entitlementsFileName, targetName);
                entitlements.AddAssociatedDomains(new string[] {"applinks:budweisergame.onelink.me"});
                entitlements.WriteToFile();
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }

            try
            {
                if (File.Exists(pathToBuildProject+"/process_symbols.sh"))
                {
                    File.WriteAllText(pathToBuildProject+"/process_symbols.sh", "");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
            Debug.Log("PostProcessBuild End");
        }
        else if (buildTarget == BuildTarget.StandaloneOSX)
        {
            AppleAuthMacosPostprocessorHelper.FixManagerBundleIdentifier(buildTarget, pathToBuildProject);
        }
    }
#endif

    static void CopyExportOptions(string pathToBuiltProject)
    {
        string sourcePath = @"Assets/Plugins/iOS/exportOptions.plist";
        string targetCachePath = pathToBuiltProject + @"/exportOptions.plist";
        try
        {
            Directory.CreateDirectory(pathToBuiltProject);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        FileUtil.CopyFileOrDirectory(sourcePath, targetCachePath);
    }

    static void RevertAppVersionObject(string backup)
    {
        File.WriteAllText(_splashSceneFilePath, backup);
    }
}


public class AndroidPostBuildProcessor : IPostGenerateGradleAndroidProject
{
    public int callbackOrder
    {
        get { return 999; }
    }

    void IPostGenerateGradleAndroidProject.OnPostGenerateGradleAndroidProject(string path)
    {
        return;
        Debug.Log("Bulid path : " + path);
        string gradlePropertiesFile = path + "/gradle.properties";
        if (File.Exists(gradlePropertiesFile))
        {
            File.Delete(gradlePropertiesFile);
        }

        StreamWriter writer = File.CreateText(gradlePropertiesFile);
        writer.WriteLine("org.gradle.jvmargs=-Xmx4096M");
        writer.WriteLine("android.useAndroidX=true");
        writer.WriteLine("android.enableJetifier=true");
        writer.Flush();
        writer.Close();
    }
}