using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using System;

namespace CalAucXpCut
{
    public class CalAucXpCutSettings : AttributeGlobalSettings<CalAucXpCutSettings>
    {
        public override string Id => "CalAucXpCutSettings";
        public override string DisplayName => "CalAuc XP Cut";
        public override string FormatType => "json";

        private bool _enableMod = true;

        [SettingPropertyGroup("{=CalAucXpCut_Group_General}General", GroupOrder = 0)]
        [SettingPropertyBool("{=CalAucXpCut_EnableMod_Name}Enable Mod", Order = 0, RequireRestart = false,
            HintText = "{=CalAucXpCut_EnableMod_Hint}Master switch.")]
        public bool EnableMod
        {
            get { try { return _enableMod; } catch { return false; } }
            set { try { if (_enableMod != value) { _enableMod = value; OnPropertyChanged(nameof(EnableMod)); } } catch { } }
        }

        private int _xpDivisor = 100;

        [SettingPropertyGroup("{=CalAucXpCut_Group_General}General", GroupOrder = 0)]
        [SettingPropertyInteger("{=CalAucXpCut_XpDivisor_Name}XP Divisor", 1, 500, Order = 1, RequireRestart = false,
            HintText = "{=CalAucXpCut_XpDivisor_Hint}How many times to cut Trade XP from auction transactions (buying and selling). 1 = vanilla, 100 = balanced, 500 = almost disabled.")]
        public int XpDivisor
        {
            get
            {
                try
                {
                    if (_xpDivisor < 1) return 1;
                    if (_xpDivisor > 500) return 500;
                    return _xpDivisor;
                }
                catch { return 100; }
            }
            set
            {
                try
                {
                    int v = value;
                    if (v < 1) v = 1;
                    if (v > 500) v = 500;
                    if (_xpDivisor != v) { _xpDivisor = v; OnPropertyChanged(nameof(XpDivisor)); }
                }
                catch { }
            }
        }

        private bool _fixConsignExploit = true;

        [SettingPropertyGroup("{=CalAucXpCut_Group_General}General", GroupOrder = 0)]
        [SettingPropertyBool("{=CalAucXpCut_FixConsign_Name}XP Only When Sold", Order = 2, RequireRestart = false,
            HintText = "{=CalAucXpCut_FixConsign_Hint}XP is granted only when the lot is actually sold. Disable to restore the old mod behaviour.")]
        public bool FixConsignExploit
        {
            get { try { return _fixConsignExploit; } catch { return true; } }
            set
            {
                try
                {
                    if (_fixConsignExploit != value)
                    {
                        _fixConsignExploit = value;
                        OnPropertyChanged(nameof(FixConsignExploit));
                    }
                }
                catch { }
            }
        }

        private bool _allowCraftedSales = false;

        [SettingPropertyGroup("{=CalAucXpCut_Group_General}General", GroupOrder = 0)]
        [SettingPropertyBool("{=CalAucXpCut_AllowCrafted_Name}Crafted Items Can Be Consigned", Order = 3, RequireRestart = false,
            HintText = "{=CalAucXpCut_AllowCrafted_Hint}Allow consigning player-crafted weapons and armor. Vanilla Calradia Auction forbids this.")]
        public bool AllowCraftedSales
        {
            get { try { return _allowCraftedSales; } catch { return false; } }
            set
            {
                try
                {
                    if (_allowCraftedSales != value)
                    {
                        _allowCraftedSales = value;
                        OnPropertyChanged(nameof(AllowCraftedSales));
                    }
                }
                catch { }
            }
        }

        private bool _showChatMessages = true;

        [SettingPropertyGroup("{=CalAucXpCut_Group_General}General", GroupOrder = 0)]
        [SettingPropertyBool("{=CalAucXpCut_ShowChatMessages_Name}Chat Messages", Order = 4, RequireRestart = false,
            HintText = "{=CalAucXpCut_ShowChatMessages_Hint}Show Trade XP events in the in-game chat.")]
        public bool ShowChatMessages
        {
            get { try { return _showChatMessages; } catch { return true; } }
            set
            {
                try
                {
                    if (_showChatMessages != value)
                    {
                        _showChatMessages = value;
                        OnPropertyChanged(nameof(ShowChatMessages));
                    }
                }
                catch { }
            }
        }

        private bool _writeXpLog = false;

        [SettingPropertyGroup("{=CalAucXpCut_Group_General}General", GroupOrder = 0)]
        [SettingPropertyBool("{=CalAucXpCut_WriteXpLog_Name}Write Log File", Order = 5, RequireRestart = false,
            HintText = "{=CalAucXpCut_WriteXpLog_Hint}Write Trade XP events to xp.log. For diagnostics.")]
        public bool WriteXpLog
        {
            get { try { return _writeXpLog; } catch { return false; } }
            set
            {
                try
                {
                    if (_writeXpLog != value)
                    {
                        _writeXpLog = value;
                        OnPropertyChanged(nameof(WriteXpLog));
                    }
                }
                catch { }
            }
        }
    }
}