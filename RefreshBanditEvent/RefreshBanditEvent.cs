using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Categories;
using TaleWorlds.CampaignSystem.Extensions;
using Helpers;
using static System.Collections.Specialized.BitVector32;
using System.Security.Claims;


namespace RefreshBanditEvent
{
    public class RefreshBanditEvent: MBSubModuleBase
    {
        protected override void OnGameStart(Game game, IGameStarter gamestarter)
        {
            if (game.GameType is Campaign)
            {
                CampaignGameStarter starter = (CampaignGameStarter)gamestarter;
                starter.AddBehavior(new BanditsCampaignBehavior());
                starter.AddBehavior(new CampaignRefreshBanditEvent());
            }
        }
    }

    public class CampaignRefreshBanditEvent : CampaignBehaviorBase
    {
        const int partyCelingAmount = 2000;
        const int newPartyAmount = 300;
        const int minDayCount = 5;
        const int maxDayCount = 14;
        const float weeklyTriggerProbability = 0.4f;

        int hourCount = 0;
        bool isProcessing = false;
        int remainDayCount = 0;

        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, BanditHourlyTick);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, BanditDailyTick);
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, BanditWeeklyTick);
        }
        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_refresh_bandit_event_hour_count", ref hourCount);
            dataStore.SyncData("_refresh_bandit_event_is_processing", ref isProcessing);
            dataStore.SyncData("_refresh_bandit_event_remain_day_count", ref remainDayCount);
        }

        public void BanditWeeklyTick()
        {
            if (isProcessing) {
                return;
            }

            float randomValue = MBRandom.RandomFloat;
            if (randomValue > weeklyTriggerProbability)
            {
                return;
            }

            remainDayCount = MBRandom.RandomInt(minDayCount, maxDayCount);
            InformationManager.DisplayMessage(new InformationMessage($"远方传来消息，人心难测，匪盗横行！一场新的匪患开始了！有人预计这次匪患会维持 {remainDayCount} 天！", Colors.Red));
            isProcessing = true;
        }

        public void BanditDailyTick()
        {
            if (isProcessing)
            {
                remainDayCount -= 1;
                if (remainDayCount <= 0)
                {
                    InformationManager.DisplayMessage(new InformationMessage($"远方传来消息，匪患过于严重，事情出现转机，现在没有人愿意加入匪患行列了！", Colors.Green));
                    isProcessing = false;
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage($"匪患仍在流行！更多人加入匪盗行列！", Colors.Yellow));
                }
            }
        }

        public void BanditHourlyTick()
        {
            if (!isProcessing)
            {
                return;
            }

            hourCount += 1;
            if (hourCount < 5) 
            {
                return;
            }

            List<Clan> clans = new List<Clan>();
            foreach (Clan clan in Clan.All)
            {
                if (clan.IsBanditFaction)
                {
                    clans.Add(clan);
                }
            }

            int currentPartyAmount = Campaign.Current.BanditParties.Count;
            for (int i = 0; i< newPartyAmount; i++ )
            {
                if (currentPartyAmount > partyCelingAmount) {
                    break;
                }
                if (CreateBanditParty(clans.GetRandomElement()))
                {
                    currentPartyAmount += 1;
                }
            }

            hourCount = 0;
        }

        // BanditsCampaignBehavior 里面的创建强盗逻辑流程，用到的接口都没暴露出来，直接抄一波流程
        private bool CreateBanditParty(Clan clan)
        {
            Settlement settlement = null;
            if (!clan.Culture.CanHaveSettlement)
            {
                List<Settlement> townsAndVillages = new List<Settlement>();
                foreach (Settlement item in Settlement.All)
                {
                    if (item.IsTown || item.IsVillage)
                    {
                        townsAndVillages.Add(item);
                    }
                }
                if (townsAndVillages.Count > 0)
                {
                    settlement = townsAndVillages.GetRandomElement();
                }
            }
            else
            {
                settlement = Hideout.All.GetRandomElement()?.Owner.Settlement;
            }

            MobileParty mobileParty = (settlement.IsHideout ? BanditPartyComponent.CreateBanditParty(clan.StringId + "_1", clan, settlement.Hideout, isBossParty: false) : BanditPartyComponent.CreateLooterParty(clan.StringId + "_1", clan, settlement, isBossParty: false));
            if (settlement == null)
            {
                return false;
            }

            mobileParty.InitializeMobilePartyAroundPosition(clan.DefaultPartyTemplate, settlement.GatePosition, 0.2f);
            mobileParty.Party.SetVisualAsDirty();
            mobileParty.ActualClan = clan;
            int initialGold = (int)(10f * (float)mobileParty.Party.MemberRoster.TotalManCount * (0.5f + 1f * MBRandom.RandomFloat));
            mobileParty.InitializePartyTrade(initialGold);
            foreach (ItemObject item in Items.All)
            {
                if (item.IsFood)
                {
                    int num = !mobileParty.MapFaction.Culture.CanHaveSettlement ? 8 : 16;
                    int num2 = MBRandom.RoundRandomized((float)mobileParty.MemberRoster.TotalManCount * (1f / (float)item.Value) * (float)num * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat);
                    if (num2 > 0)
                    {
                        mobileParty.ItemRoster.AddToCounts(item, num2);
                    }
                }
            }
            mobileParty.Aggressiveness = 1f - 0.2f * MBRandom.RandomFloat;
            mobileParty.Ai.SetMovePatrolAroundPoint(settlement.IsTown ? settlement.GatePosition : settlement.Position2D);
            return true;
        }
    }
}
