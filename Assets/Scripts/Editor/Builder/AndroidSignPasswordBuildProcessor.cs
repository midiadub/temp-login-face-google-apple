using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Midiadub.Editor.Builder
{
    class AndroidSignPasswordBuildProcessor// : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }
        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.Android)
            {
                global::Builder.SetSignPassword();
            }
        }
    }
}