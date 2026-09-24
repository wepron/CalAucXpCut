using HarmonyLib;
using System;
using System.IO;
using CalradiaAuction.Behaviors;
using CalradiaAuction.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace CalAucXpCut
{
    // =========================================================================
    // CalAucXpCut — cuts Trade XP from Calradia Auction
    // =========================================================================
    // Patches:
    //   1. AuctionPricing.AwardTradeXp — divides XP by XpDivisor.
    //   2. AuctionConsignmentFlow.SellStack — suppresses XP during consignment,
    //      so XP is not granted at the moment of listing.
    //   3. AuctionBiddingFlow.PayConsignor — awards XP when the lot is
    //      actually sold (player received money).
    //   4. AuctionCatalog.IsSellable — allows selling crafted items if the
    //      corresponding setting is enabled.
    // =========================================================================

    public class CalAucXpCutSubModule : MBSubModuleBase
    {
        public static CalAucXpCutSubModule Current { get; private set; }

        public static volatile bool SuppressAwardXp = false;

        private const string ConfigFolderName = "CalAucXpCut";

        private static string ConfigDir
        {
            get
            {
                try
                {
                    string dir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "Mount and Blade II Bannerlord",
                        "Configs",
                        ConfigFolderName);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    return dir;
                }
                catch { return null; }
            }
        }

        private static string ErrorLogPath
        {
            get
            {
                try { var d = ConfigDir; return d == null ? null : Path.Combine(d, "errors.log"); }
                catch { return null; }
            }
        }

        private static string XpLogPath
        {
            get
            {
                try { var d = ConfigDir; return d == null ? null : Path.Combine(d, "xp.log"); }
                catch { return null; }
            }
        }

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            Current = this;

            try
            {
                var harmony = new Harmony("com.calacuxpcut.patch");
                harmony.PatchAll(typeof(CalAucXpCutSubModule).Assembly);
                Log("Harmony patches applied.");
            }
            catch (Exception ex)
            {
                Log("OnSubModuleLoad error: " + ex);
            }
        }

        // Always written to errors.log regardless of settings.
        public static void Log(string message)
        {
            try
            {
                var p = ErrorLogPath;
                if (p == null) return;
                File.AppendAllText(p, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}\n");
            }
            catch { }
        }

        // Diagnostic file log. Gated by settings.WriteXpLog.
        public static void LogXp(string message)
        {
            try
            {
                var s = CalAucXpCutSettings.Instance;
                if (s != null && !s.WriteXpLog) return;

                var p = XpLogPath;
                if (p == null) return;
                File.AppendAllText(p, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}\n");
            }
            catch { }
        }

        // Player-facing chat message. Gated by settings.ShowChatMessages.
        // Key must exist in ModuleData/Languages/.../std_CalAucXpCut_strings.xml
        public static void Msg(string key, params (string Name, string Value)[] args)
        {
            try
            {
                var s = CalAucXpCutSettings.Instance;
                if (s != null && !s.ShowChatMessages) return;

                var text = new TextObject("{=" + key + "}");
                if (args != null)
                {
                    foreach (var (name, value) in args)
                    {
                        text.SetTextVariable(name, value);
                    }
                }
                InformationManager.DisplayMessage(new InformationMessage(text.ToString(), Colors.Cyan));
            }
            catch { }
        }
    }

    // =========================================================================
    // PATCH 1: AuctionPricing.AwardTradeXp(int denars)
    // =========================================================================
    [HarmonyPatch(typeof(AuctionPricing), "AwardTradeXp")]
    public static class Patch_AwardTradeXp
    {
        [HarmonyPrefix]
        public static void Prefix(ref int denars)
        {
            try
            {
                var s = CalAucXpCutSettings.Instance;
                if (s == null || !s.EnableMod)
                {
                    CalAucXpCutSubModule.SuppressAwardXp = false;
                    return;
                }

                if (s.FixConsignExploit && CalAucXpCutSubModule.SuppressAwardXp)
                {
                    CalAucXpCutSubModule.LogXp($"[AwardTradeXp] SUPPRESS (consign) denars={denars}");
                    denars = 0;
                    return;
                }

                int divisor = s.XpDivisor;
                if (divisor <= 1) return;
                if (Campaign.Current == null) return;
                if (denars <= 0) return;

                int orig = denars;
                int res = denars / divisor;
                if (res <= 0) res = 1;

                denars = res;

                float xp = res * 0.05f;

                CalAucXpCutSubModule.LogXp($"[AwardTradeXp] {orig} -> {res} (divisor={divisor}) -> +{xp:F2} XP");
                CalAucXpCutSubModule.Msg("CalAucXpCut_Msg_AwardTradeXp",
                    ("XP", xp.ToString("F1")));
            }
            catch (Exception ex)
            {
                CalAucXpCutSubModule.Log("AwardTradeXp.Prefix: " + ex.Message);
            }
        }
    }

    // =========================================================================
    // PATCH 2: AuctionConsignmentFlow.SellStack
    // =========================================================================
    [HarmonyPatch(typeof(AuctionConsignmentFlow), "SellStack")]
    public static class Patch_SellStack
    {
        [HarmonyPrefix]
        public static void Prefix(bool immediate)
        {
            try
            {
                CalAucXpCutSubModule.SuppressAwardXp = false;

                var s = CalAucXpCutSettings.Instance;
                if (s == null || !s.EnableMod) return;
                if (!s.FixConsignExploit) return;

                if (!immediate)
                {
                    CalAucXpCutSubModule.SuppressAwardXp = true;
                    CalAucXpCutSubModule.LogXp("[SellStack] immediate=false -> SUPPRESS set");
                }
                else
                {
                    CalAucXpCutSubModule.LogXp("[SellStack] immediate=true -> no suppress");
                }
            }
            catch (Exception ex)
            {
                CalAucXpCutSubModule.SuppressAwardXp = false;
                CalAucXpCutSubModule.Log("SellStack.Prefix: " + ex.Message);
            }
        }

        [HarmonyPostfix]
        public static void Postfix()
        {
            try { CalAucXpCutSubModule.SuppressAwardXp = false; }
            catch { }
        }
    }

    // =========================================================================
    // PATCH 3: AuctionBiddingFlow.PayConsignor
    // =========================================================================
    [HarmonyPatch(typeof(AuctionBiddingFlow), "PayConsignor")]
    public static class Patch_PayConsignor
    {
        [HarmonyPostfix]
        public static void Postfix(AuctionLot lot, int hammerPrice)
        {
            try
            {
                CalAucXpCutSubModule.LogXp($"[PayConsignor] CALLED lot={(lot == null ? "null" : "ok")} hammerPrice={hammerPrice}");

                var s = CalAucXpCutSettings.Instance;
                if (s == null || !s.EnableMod) { CalAucXpCutSubModule.LogXp("  skip: mod disabled"); return; }
                if (!s.FixConsignExploit) { CalAucXpCutSubModule.LogXp("  skip: fix disabled"); return; }
                if (lot == null || hammerPrice <= 0) { CalAucXpCutSubModule.LogXp("  skip: null/neg"); return; }
                if (Campaign.Current == null || Hero.MainHero == null) { CalAucXpCutSubModule.LogXp("  skip: no Campaign/Hero"); return; }

                bool isPlayerConsigned = false;
                try { isPlayerConsigned = lot.IsPlayerConsigned; }
                catch (Exception ex) { CalAucXpCutSubModule.LogXp("  error reading IsPlayerConsigned: " + ex.Message); }

                CalAucXpCutSubModule.LogXp($"  IsPlayerConsigned={isPlayerConsigned}");

                if (!isPlayerConsigned) return;

                float feeRate = AuctionConfig.ConsignmentFeeRate;
                int payout = (int)((float)hammerPrice * (1f - feeRate));
                if (payout <= 0) return;

                int divisor = s.XpDivisor;
                int effective = payout;
                if (divisor > 1)
                {
                    effective = payout / divisor;
                    if (effective <= 0) effective = 1;
                }

                float xpToAdd = effective * 0.05f;
                Hero.MainHero.AddSkillXp(DefaultSkills.Trade, xpToAdd);

                CalAucXpCutSubModule.LogXp($"  AWARD: hammer={hammerPrice}, payout={payout}, div={divisor}, effective={effective}, baseXp={xpToAdd:F2}");
                CalAucXpCutSubModule.Msg("CalAucXpCut_Msg_ConsignSold",
                    ("XP", xpToAdd.ToString("F1")));
            }
            catch (Exception ex)
            {
                CalAucXpCutSubModule.Log("PayConsignor.Postfix: " + ex.Message);
            }
        }
    }

    // =========================================================================
    // PATCH 4: AuctionCatalog.IsSellable(ItemObject item)
    // =========================================================================
    // Original returns false for crafted items (item.IsCraftedByPlayer).
    // We intercept this case and, if AllowCraftedSales = true, return true —
    // but only if the item passes ALL other criteria the original checks.
    //
    // For non-crafted items we do nothing and let the original run as-is.
    // =========================================================================

    [HarmonyPatch(typeof(AuctionCatalog), "IsSellable")]
    public static class Patch_IsSellable
    {
        [HarmonyPrefix]
        public static bool Prefix(ItemObject item, ref bool __result)
        {
            try
            {
                if (item == null) return true;
                if (!item.IsCraftedByPlayer) return true;

                var s = CalAucXpCutSettings.Instance;
                if (s == null || !s.EnableMod) return true;
                if (!s.AllowCraftedSales) return true;

                if (item.IsBannerItem)
                {
                    __result = false;
                    return false;
                }

                if (item.IsTradeGood || item.IsAnimal)
                {
                    __result = AuctionConfig.TradeGoodsEnabled
                                && AuctionConfig.GoodsTypes.Contains(item.ItemType);
                    return false;
                }

                if (!AuctionConfig.TradedTypes.Contains(item.ItemType))
                {
                    __result = false;
                    return false;
                }

                if (item.NotMerchandise && !AuctionConfig.RestrictedSaleExemptions.Contains(item.ItemType))
                {
                    __result = false;
                    return false;
                }

                __result = true;
                return false;
            }
            catch (Exception ex)
            {
                CalAucXpCutSubModule.Log("IsSellable.Prefix: " + ex.Message);
                return true;
            }
        }
    }
}