using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrewChiefV3
{
    class UserSettings
    {
        private String[] reservedNameStarts = new String[] { "CHANNEL_", "TOGGLE_", "VOICE_OPTION", "background_volume", "messages_volume" };
        public Dictionary<String, String> propertyHelp = new Dictionary<String, String>();
        private UserSettings()
        {
            propertyHelp.Add("sound_files_path", "The path (relative to CrewChiefV3.exe) of the sound pack you want to use");
            propertyHelp.Add("background_volume", "The volume of the background sounds (0 - 1)");
            propertyHelp.Add("update_interval", "The time (milliseconds) between app updates");
            propertyHelp.Add("use_sweary_messages", "A few messages contain swearing - then enables / disables these");
            propertyHelp.Add("enable_spotter", "The spotter can be enabled and disabled with a button. This setting sets it initial state");
            propertyHelp.Add("speech_recognition_location", "The localisation to use for speech recognition. Must be en-[something]");
            propertyHelp.Add("spotter_car_length", "The length of a car, used to check if there's an overlap. Decrease this if the spotter calls 'hold your line' when you're not overlapping. "+
                "Increase it if the spotter doesn't call 'hold your line' when you clearly are overlapping");
            propertyHelp.Add("time_after_race_start_for_spotter", "Wait this many seconds after race start before enabling the spotter");
            propertyHelp.Add("min_speed_for_spotter", "Don't use the spotter if your speed is less than this");
            propertyHelp.Add("max_closing_speed_for_spotter", "Don't call 'hold your line' if the closing speed between you and the other car is greater than this");
            propertyHelp.Add("spotter_only_when_being_passed", "Only 'spot' for cars overtaking you");
            propertyHelp.Add("spotter_clear_delay", "You need to be clear for this many milliseconds before the spotter calls 'clear'");
            propertyHelp.Add("spotter_overlap_delay", "You need to be overlapping for this many milliseconds before the spotter calls 'hold your line'");
            propertyHelp.Add("read_lap_times", "Occasionally read out the player's laptimes when crossing the line");
            propertyHelp.Add("custom_device_guid", "Manually set a controller GUID if the app doesn't display your controller in the devices list");
            propertyHelp.Add("disable_immediate_messages", "Disables all spotter messages and all voice recognition responses. " +
                "Might allow the app to run in non-interactive mode on slow systems");
            propertyHelp.Add("max_safe_oil_temp_over_baseline", "Baseline oil temp is taken after a few minutes. If the oil temp goes more than this value over the baseline " +
                "a warning message is played. Reduce this to make the 'high oil temp' warning more sensitive");
            propertyHelp.Add("max_safe_water_temp_over_baseline", "Baseline water temp is taken after a few minutes. If the water temp goes more than this value over the baseline " +
                "a warning message is played. Reduce this to make the 'high water temp' warning more sensitive");
            propertyHelp.Add("r3e_launch_params", "This is used to tell Steam what app to run - raceroom's Steam app ID is 211500. If you're using a non-Steam version of RaceRoom this can be empty");
            propertyHelp.Add("r3e_launch_exe", "This is the program used to start RaceRoom. For most users this should be the full path to their steam.exe "+
                "(e.g. C:/program files/steam/steam.exe). Use forward slashes to separate paths. If you have a non-steam version of RaceRoom use the full path to the RaceRoom exe");
            propertyHelp.Add("launch_raceroom", "If this is true the application will attempt to launch RaceRoom when CrewChief starts (when you click 'Start application'" + 
                " or the app auto starts CrewChief if you set run_immediately to true)");
            propertyHelp.Add("run_immediately", "If this is true the application will start running CrewChief as soon as you start it up, using whatever options you previously set");
        }

        public String getHelp(String propertyId)
        {
            if (propertyHelp.ContainsKey(propertyId))
            {
                return propertyHelp[propertyId];
            }
            else
            {
                return "";
            }
        }

        public List<SettingsProperty> getProperties(Type requiredType, String nameMustStartWith, String nameMustNotStartWith)
        {
            List<SettingsProperty> props = new List<SettingsProperty>();
            foreach (SettingsProperty prop in Properties.Settings.Default.Properties)
            {
                Boolean isReserved = false;
                foreach (String reservedNameStart in reservedNameStarts)
                {
                    if (prop.Name.StartsWith(reservedNameStart))
                    {
                        isReserved = true;
                        break;
                    }
                }
                if (!isReserved && 
                    (nameMustStartWith == null || nameMustStartWith.Length == 0 || prop.Name.StartsWith(nameMustStartWith)) &&
                    (nameMustNotStartWith == null || nameMustNotStartWith.Length == 0 || !prop.Name.StartsWith(nameMustNotStartWith)) &&
                    !prop.IsReadOnly && prop.PropertyType == requiredType)
                {
                    props.Add(prop);
                }
            }
            return props.OrderBy(x => x.Name).ToList();
        }

        private static readonly UserSettings _userSettings = new UserSettings();

        private Boolean propertiesUpdated = false;

        public static UserSettings GetUserSettings()
        {
            return _userSettings;
        }

        public String getString(String name)
        {
            return (String)Properties.Settings.Default[name];
        }

        public float getFloat(String name)
        {
            return (float) Properties.Settings.Default[name];
        }

        public Boolean getBoolean(String name)
        {
            return (Boolean)Properties.Settings.Default[name];
        }

        public int getInt(String name)
        {
            return (int)Properties.Settings.Default[name];
        }

        public void setProperty(String name, Object value)
        {
            if (value != Properties.Settings.Default[name])
            {
                Properties.Settings.Default[name] = value;
                propertiesUpdated = true;
            }
        }

        public void saveUserSettings()
        {
            if (propertiesUpdated)
            {
                Properties.Settings.Default.Save();
            }
        }
    }
}
