using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Android.Content;
using Android.Content.PM;
using Xamarin.Forms;
using AChatFull.Services;
using AChatFull.Droid.Services;

[assembly: Dependency(typeof(IconSwitchService))]
namespace AChatFull.Droid.Services
{
    public class IconSwitchService : IIconSwitchService
    {
        public Task SwitchAppIcon(int iconType)
        {
            Debug.WriteLine("SwitchAppIcon: "+ iconType);

            return Task.Run(() =>
            {
                try
                {
                    var context = Android.App.Application.Context;
                    var pm = context.PackageManager;
                    var packageName = context.PackageName;

                    if(iconType == 1)
                    {
                        pm.SetComponentEnabledSetting(
                            new ComponentName(packageName, "com.companyname.achatfull.MainActivity"),
                            ComponentEnabledState.Disabled,
                            ComponentEnableOption.DontKillApp
                        );

                        pm.SetComponentEnabledSetting(
                             new ComponentName(packageName, "com.companyname.achatfull.MainActivity_Old"),
                             ComponentEnabledState.Enabled,
                             ComponentEnableOption.DontKillApp
                        );
                    }
                    else
                    {
                        pm.SetComponentEnabledSetting(
                            new ComponentName(packageName, "com.companyname.achatfull.MainActivity"),
                            ComponentEnabledState.Enabled,
                            ComponentEnableOption.DontKillApp
                        );
                        
                        pm.SetComponentEnabledSetting(
                             new ComponentName(packageName, "com.companyname.achatfull.MainActivity_Old"),
                             ComponentEnabledState.Disabled,
                             ComponentEnableOption.DontKillApp
                        );
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[IconSwitchService-Android] Error: {ex}");
                }
            });
        }
    }
}