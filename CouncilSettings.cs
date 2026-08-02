using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

namespace SmallCouncils.Settings
{
    /// <summary>
    /// MCM v5 (Aragas/BUTR) global settings for Small Councils. Every
    /// previously-hardcoded magic number from Stages 3-4 lives here now —
    /// CouncilConstants, KingsguardBonusService, and
    /// CouncilHealingAndMoraleBonusService all read from
    /// CouncilSettings.Instance instead of local constants (falling back to
    /// the same defaults that were hardcoded before, via ?? in each call
    /// site, in case Instance is ever null before MCM finishes loading).
    ///
    /// VERIFY BEFORE COMPILE: the base class's Id/DisplayName/FolderName/
    /// FormatType override pattern below is the standard, extremely common
    /// MCM v5 convention used across the wider modding community, but
    /// wasn't directly confirmed via reflection against your exact
    /// AttributeGlobalSettings&lt;T&gt; (a deeper interface-based hierarchy
    /// than a shallow DeclaredOnly reflection pass surfaced). The
    /// SettingProperty* attribute constructors and their Order/HintText/
    /// RequireRestart/IsToggle properties ARE confirmed via reflection
    /// against your MCMv5.dll.
    /// </summary>
    public class CouncilSettings : AttributeGlobalSettings<CouncilSettings>
    {
        public override string Id => "SmallCouncils_v1";
        public override string DisplayName => "Small Councils";
        public override string FolderName => "SmallCouncils";
        public override string FormatType => "json";

        // ============================================================
        // General
        // ============================================================

        [SettingPropertyInteger("Negative relation removal threshold", -100, 100, HintText = "AI kingdoms remove a council member (except the Lord Commander) once their relation with the ruler falls to or below this value.", Order = 1)]
        [SettingPropertyGroup("General")]
        public int NegativeRelationThreshold { get; set; } = -20;

        [SettingPropertyInteger("Hand of the King re-evaluation interval (weeks)", 1, 52, HintText = "How often AI kingdoms reconsider whether a different clan leader would make a better Hand of the King.", Order = 2)]
        [SettingPropertyGroup("General")]
        public int HandReevaluationIntervalWeeks { get; set; } = 12;

        // ============================================================
        // Relations — gained on assignment
        // ============================================================

        [SettingPropertyInteger("Hand of the King", 0, 200, Order = 1)]
        [SettingPropertyGroup("Relations.Gained on Assignment")]
        public int RelationGain_HandOfTheKing { get; set; } = 100;

        [SettingPropertyInteger("Grand Maester", 0, 200, Order = 2)]
        [SettingPropertyGroup("Relations.Gained on Assignment")]
        public int RelationGain_GrandMaester { get; set; } = 20;

        [SettingPropertyInteger("Master of Coin", 0, 200, Order = 3)]
        [SettingPropertyGroup("Relations.Gained on Assignment")]
        public int RelationGain_MasterOfCoin { get; set; } = 30;

        [SettingPropertyInteger("Master of Laws", 0, 200, Order = 4)]
        [SettingPropertyGroup("Relations.Gained on Assignment")]
        public int RelationGain_MasterOfLaws { get; set; } = 30;

        [SettingPropertyInteger("Master of Ships", 0, 200, Order = 5)]
        [SettingPropertyGroup("Relations.Gained on Assignment")]
        public int RelationGain_MasterOfShips { get; set; } = 30;

        [SettingPropertyInteger("Master of Whisperers", 0, 200, Order = 6)]
        [SettingPropertyGroup("Relations.Gained on Assignment")]
        public int RelationGain_MasterOfWhisperers { get; set; } = 20;

        [SettingPropertyInteger("Lord Commander of the Kingsguard", 0, 200, Order = 7)]
        [SettingPropertyGroup("Relations.Gained on Assignment")]
        public int RelationGain_LordCommanderOfKingsguard { get; set; } = 100;

        // ============================================================
        // Relations — lost on unassignment
        // ============================================================

        [SettingPropertyInteger("Hand of the King", -200, 0, Order = 1)]
        [SettingPropertyGroup("Relations.Lost on Unassignment")]
        public int RelationLoss_HandOfTheKing { get; set; } = -50;

        [SettingPropertyInteger("Grand Maester", -200, 0, Order = 2)]
        [SettingPropertyGroup("Relations.Lost on Unassignment")]
        public int RelationLoss_GrandMaester { get; set; } = -20;

        [SettingPropertyInteger("Master of Coin", -200, 0, Order = 3)]
        [SettingPropertyGroup("Relations.Lost on Unassignment")]
        public int RelationLoss_MasterOfCoin { get; set; } = -30;

        [SettingPropertyInteger("Master of Laws", -200, 0, Order = 4)]
        [SettingPropertyGroup("Relations.Lost on Unassignment")]
        public int RelationLoss_MasterOfLaws { get; set; } = -30;

        [SettingPropertyInteger("Master of Ships", -200, 0, Order = 5)]
        [SettingPropertyGroup("Relations.Lost on Unassignment")]
        public int RelationLoss_MasterOfShips { get; set; } = -30;

        [SettingPropertyInteger("Master of Whisperers", -200, 0, Order = 6)]
        [SettingPropertyGroup("Relations.Lost on Unassignment")]
        public int RelationLoss_MasterOfWhisperers { get; set; } = -20;

        [SettingPropertyInteger("Lord Commander of the Kingsguard", -200, 0, Order = 7)]
        [SettingPropertyGroup("Relations.Lost on Unassignment")]
        public int RelationLoss_LordCommanderOfKingsguard { get; set; } = -5;

        // ============================================================
        // Master of Coin
        // ============================================================

        [SettingPropertyInteger("Officeholder's daily personal gold", 0, 50000, HintText = "Flat gold given to the Master of Coin themself each day.", Order = 1)]
        [SettingPropertyGroup("Benefits.Master of Coin")]
        public int MasterOfCoinPersonalGold { get; set; } = 10000;

        [SettingPropertyFloatingInteger("Ruler's gold per Steward skill point", 0f, 100f, "#0.0", HintText = "Daily gold given to the ruler, multiplied by the Master of Coin's Steward skill.", Order = 2)]
        [SettingPropertyGroup("Benefits.Master of Coin")]
        public float MasterOfCoinClanIncomeMultiplier { get; set; } = 20f;

        // ============================================================
        // Master of Whisperers
        // ============================================================

        [SettingPropertyFloatingInteger("Weekly relation gain per Roguery skill point", 0f, 1f, "#0.00", HintText = "Weekly relation gain between the ruler and a random lord, multiplied by the Master of Whisperers' Roguery skill.", Order = 1)]
        [SettingPropertyGroup("Benefits.Master of Whisperers")]
        public float MasterOfWhisperersRelationGainMultiplier { get; set; } = 0.05f;

        // ============================================================
        // Lord Commander of the Kingsguard
        // ============================================================

        [SettingPropertyFloatingInteger("Kingsguard members' skill XP multiplier", 1f, 10f, "#0.0", HintText = "Multiplies all skill XP gained by ordinary Kingsguard roster members (not the Lord Commander, who has their own separate multiplier below).", Order = 1)]
        [SettingPropertyGroup("Benefits.Lord Commander of the Kingsguard")]
        public float KingsguardXpMultiplier { get; set; } = 2f;

        [SettingPropertyFloatingInteger("Lord Commander's own skill XP multiplier", 1f, 10f, "#0.0", HintText = "Multiplies all skill XP gained by the Lord Commander themself.", Order = 2)]
        [SettingPropertyGroup("Benefits.Lord Commander of the Kingsguard")]
        public float LordCommanderXpMultiplier { get; set; } = 3f;

        [SettingPropertyFloatingInteger("Ruler's party morale bonus per vigor point", 0f, 1f, "#0.00", HintText = "Morale bonus to the ruler's own party, multiplied by the Lord Commander's vigor.", Order = 3)]
        [SettingPropertyGroup("Benefits.Lord Commander of the Kingsguard")]
        public float LordCommanderMoraleBonusMultiplier { get; set; } = 0.05f;

        // ============================================================
        // Grand Maester
        // ============================================================

        [SettingPropertyFloatingInteger("Max hit points bonus per Medicine skill point", 0f, 5f, "#0.00", HintText = "Extra max hit points for the kingdom's ruler, multiplied by the Grand Maester's Medicine skill.", Order = 1)]
        [SettingPropertyGroup("Benefits.Grand Maester")]
        public float GrandMaesterMedicineBonusMultiplier { get; set; } = 0.05f;

        // ============================================================
        // Hand of the King
        // ============================================================

        [SettingPropertyInteger("Ruler's daily influence, as % of the Hand's clan influence", 0, 50, HintText = "Daily influence given to the ruler's clan, as a whole-number percentage of the Hand of the King's own clan's current influence. E.g. 1 = 1%.", Order = 1)]
        [SettingPropertyGroup("Benefits.Hand of the King")]
        public int HandOfTheKingInfluencePercentPoints { get; set; } = 3;

        // ============================================================
        // Master of Laws
        // ============================================================

        [SettingPropertyFloatingInteger("Security bonus per Leadership skill point", 0f, 1f, "#0.00", HintText = "Settlement security bonus across the kingdom, multiplied by the Master of Laws' Leadership skill.", Order = 1)]
        [SettingPropertyGroup("Benefits.Master of Laws")]
        public float MasterOfLawsSecurityBonusMultiplier { get; set; } = 0.05f;

        // ============================================================
        // Master of Ships
        // ============================================================

        [SettingPropertyFloatingInteger("Naval speed bonus per 100 Shipmaster skill", 0f, 1f, "#0.00", HintText = "Naval travel speed bonus for kingdom parties at sea, multiplied by the Master of Ships' Shipmaster skill (out of 100).", Order = 1)]
        [SettingPropertyGroup("Benefits.Master of Ships")]
        public float MasterOfShipsSpeedBonusMultiplier { get; set; } = 0.1f;
    }
}
