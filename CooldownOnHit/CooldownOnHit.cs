using System;
using BepInEx;
using BepInEx.Configuration;
using RoR2;
using UnityEngine;
using R2API;
using System.Reflection;
using Utilities;

namespace CooldownOnHit
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.Modernkennnern.CooldownOnHit", "CooldownOnHit", "0.1.0")]
    public class CooldownOnHit : BaseUnityPlugin
    {
        // TODO: Redo the way cooldowns are displayed.
        // Currently, Huntress Glaive has 7second cooldown - it says 7 until you've hit enough times to get it to 6.
        // What I would like is to turn that number into a "Hits remaining" visual that, instead of telling you a (non-functioning) timer, tells you how many hits until the cooldown is off.
        // This would require a function that turns the total cooldown into a number (7 seconds = 10 hits, for example), and ideally would be different for each character. (MUL-T would be bonkers with only 10 hits, while Sniper would be incredibly bad)

        // TODO: Turn this into a (Lunar) item

        // TODO: Allow the possibility of assigning specific skillslots to other damage sources

        // TODO: Change cooldown reduction to scale with ProcCoeffiency (This is on the damage source, so it will be somewhat awkward to implement)

        // The ideal method for implementing would be: You have two parameters. The first is a GenericSkill that, when it hits an enemy, will reduce the cooldown of the second GenericSkill.

        private static ConfigWrapper<bool> PrimarySkillRechargingConfig { get; set; }
        private static ConfigWrapper<bool> SecondarySkillRechargingConfig { get; set; }
        private static ConfigWrapper<bool> UtilitySkillRechargingConfig { get; set; }
        private static ConfigWrapper<bool> SpecialSkillRechargingConfig { get; set; }
        private static ConfigWrapper<bool> EquipmentRechargingConfig { get; set; }

        private static ConfigWrapper<float> SecondarySkillCooldownReductionOnHitAmountConfig { get; set; }
        private static ConfigWrapper<float> SpecialSkillCooldownReductionOnHitAmountConfig { get; set; }
        private static ConfigWrapper<float> EquipmentCooldownReductionOnHitAmountConfig { get; set; }

        private readonly SurvivorIndex[] workingSurvivors = new SurvivorIndex[] { SurvivorIndex.Huntress };

        public float SecondaryAbilityCooldownReductionOnHitAmount {
            get => SecondarySkillCooldownReductionOnHitAmountConfig.Value;
            protected set => SecondarySkillCooldownReductionOnHitAmountConfig.Value = value;
        }

        public float SpecialAbilityCooldownReductionOnHitAmount {
            get => SpecialSkillCooldownReductionOnHitAmountConfig.Value;
            protected set => SpecialSkillCooldownReductionOnHitAmountConfig.Value = value;
        }
        public float EquipmentCooldownReductionOnHitAmount {
            get => EquipmentCooldownReductionOnHitAmountConfig.Value;
            protected set => EquipmentCooldownReductionOnHitAmountConfig.Value = value;
        }

        public bool PrimarySkillRecharging {
            get => PrimarySkillRechargingConfig.Value;
            protected set => PrimarySkillRechargingConfig.Value = value;
        }
        public bool SecondarySkillRecharging {
            get => SecondarySkillRechargingConfig.Value;
            protected set => SecondarySkillRechargingConfig.Value = value;
        }
        public bool UtilitySkillRecharging {
            get => UtilitySkillRechargingConfig.Value;
            protected set => UtilitySkillRechargingConfig.Value = value;
        }
        public bool SpecialSkillRecharging {
            get => SpecialSkillRechargingConfig.Value;
            protected set => SpecialSkillRechargingConfig.Value = value;
        }
        public bool EquipmentRecharging {
            get => EquipmentRechargingConfig.Value;
            protected set => EquipmentRechargingConfig.Value = value;
        }


        public SkillLocator SkillLoc {
            get => characterMaster.GetBody().GetComponent<SkillLocator>();
        }

        public GenericSkill PrimarySkill {
            get => SkillLoc.GetSkill(SkillSlot.Primary);
        }
        public GenericSkill SecondarySkill {
            get => SkillLoc.GetSkill(SkillSlot.Secondary);
        }
        public GenericSkill UtilitySkill {
            get => SkillLoc.GetSkill(SkillSlot.Utility);
        }
        public GenericSkill SpecialSkill {
            get => SkillLoc.GetSkill(SkillSlot.Special);
        }

        private float newRechargeStopwatch;
        private float newFinalRechargeInterval;

        private CharacterMaster characterMaster;
        private SurvivorIndex survivorIndex;

        private bool characterSupported;

        public void Awake()
        {
            SetConfigWraps();

            EnableEvents();
            ShowStats();

        }

        private void Update()
        {
            //if (Input.GetKeyDown(KeyCode.F2))
            //{
            //    CheckSupport();
            //}
        }

        private void ShowStats()
        {
            Chat.AddMessage(Stats);
        }

        private void EnableEvents()
        {
           
            // This will load at the start of every map with a Teleporter (presumably)
            On.RoR2.SceneDirector.PlaceTeleporter += (orig, self) =>
            {
                CheckCharacterMaster();
                SetStartStats();
            };

            On.RoR2.GenericSkill.RunRecharge += GenericSkill_RunRecharge;

            On.RoR2.Console.Awake += (orig, self) =>
            {
                CommandHelper.RegisterCommands(self);
                orig(self);
            };

            // Huntress
            On.RoR2.Orbs.LightningOrb.OnArrival += LightningOrb_OnArrival;
            On.RoR2.Orbs.ArrowOrb.OnArrival += ArrowOrb_OnArrival;

            // Commando

            //On.RoR2.BulletAttack.DefaultHitCallback += BulletAttack_DefaultHitCallback;

        }

        private bool BulletAttack_DefaultHitCallback(On.RoR2.BulletAttack.orig_DefaultHitCallback orig, BulletAttack self, ref BulletAttack.BulletHit hitInfo)
        {
            var b = orig(self, ref hitInfo);

            if  (!hitInfo.entityObject.GetComponent<TeamComponent>() )
            {
                return b;
            }

            if (hitInfo.entityObject.GetComponent<TeamComponent>().teamIndex == TeamIndex.Monster)
            {
                Debug.Log("Hit a monster!");
            }

            return b;
        }

        private void SetStartStats()
        {
            CheckSupport();

            newFinalRechargeInterval = float.NaN;
            newRechargeStopwatch = float.NaN;

            Debug.Log("CooldownOnHit started");
        }

        private string Stats {
             get => "Primary(" + PrimarySkillRecharging   + ")"   +"\n" +
                "Secondary("   + SecondarySkillRecharging + "): " + SecondaryAbilityCooldownReductionOnHitAmount.ToString() + "\n" +
                "Utility("     + UtilitySkillRecharging   + ")" + "\n" +
                "Special("     + SpecialSkillRecharging   + "): " + SpecialAbilityCooldownReductionOnHitAmount.ToString()   + "\n" +
                "Equipment("   + EquipmentRecharging      + ")";
    }

        private void SetConfigWraps()
        {
            PrimarySkillRechargingConfig = Config.Wrap(
                "Cooldowns",
                "PrimarySkillRecharging",
                "Enables normal recharging of primary skill",
                true);
            SecondarySkillRechargingConfig = Config.Wrap(
                "Cooldowns",
                "SecondarySkillRecharging",
                "Enables normal recharging of Secondary Skill",
                false);
            UtilitySkillRechargingConfig = Config.Wrap(
                "Cooldowns",
                "UtilitySkillRecharging",
                "Enables normal recharging of Utility skills",
                true);
            SpecialSkillRechargingConfig = Config.Wrap(
                "Cooldowns",
                "SpecialSkillRecharging",
                "Enables normal recharging of Special Skills",
                false);
            EquipmentRechargingConfig = Config.Wrap(
                "Cooldowns",
                "EquipmentRecharging",
                "W.I.P - Cannot currently be disabled with this mod (Coming in a future major update)\nEnables normal recharging of Equipment",
                true);

            SecondarySkillCooldownReductionOnHitAmountConfig = Config.Wrap(
                "Cooldowns",
                "SecondaryAbilityCooldownReductionOnHitAmount",
                "How many seconds to reduce the Secondary skill(RMB) cooldown by on each hit with the Primary skill.",
                0.5f);

            SpecialSkillCooldownReductionOnHitAmountConfig = Config.Wrap(
                "Cooldowns",
                "SpecialAbilityCooldownReductionOnHitAmount",
                "How many seconds to reduce the Special Skill(R) cooldown by on each hit with the Secondary skill.",
                2.5f);

            EquipmentCooldownReductionOnHitAmountConfig = Config.Wrap(
                "Cooldowns",
                "EquipmentCooldownReductionOnHitAmount",
                "How many seconds to reduce the Equipment(Q) cooldown by on each hit with the ??? [Special Skill feels too limiting. All skills? Would like some suggestions here",
                1f);
        }

        private void GenericSkill_RunRecharge(On.RoR2.GenericSkill.orig_RunRecharge orig, GenericSkill skill, float dt)
        {


            if (!characterSupported)
            {
                orig(skill, dt);
                return;
            }



            // If 'self' is an ability that should be recharging, do the normal RunRecharge (And await further instructions)
            if (
                (skill == PrimarySkill && PrimarySkillRecharging) ||
                (skill == SecondarySkill && SecondarySkillRecharging) ||
                (skill == UtilitySkill && UtilitySkillRecharging) ||
                (skill == SpecialSkill && SpecialSkillRecharging))
            {

                orig(skill, dt);

                // If the skill should be recharging based on hits as well, do my weird RunRecharge, otherwise return
                if (
                    (skill == SecondarySkill && SecondaryAbilityCooldownReductionOnHitAmount == 0) ||
                    (skill == SpecialSkill && SpecialAbilityCooldownReductionOnHitAmount == 0))
                {
                    return;
                }
            }

            if (skill.stock >= skill.maxStock) return;

            // Not entirely sure about this next line..
            var dt2 = Time.fixedDeltaTime;
            if (dt == dt2) return;

            //Chat.AddMessage("Secondary or Special currently on cooldown");

            var skillType = typeof(RoR2.GenericSkill);

            var rechargeStopwatchField = skillType.GetField("rechargeStopwatch", BindingFlags.NonPublic | BindingFlags.Instance);
            var finalRechargeIntervalField = skillType.GetField("finalRechargeInterval", BindingFlags.NonPublic | BindingFlags.Instance);
            var restockSteplikeMethod = skillType.GetMethod("RestockSteplike", BindingFlags.NonPublic | BindingFlags.Instance);

            if (newRechargeStopwatch == float.NaN) newRechargeStopwatch = GetPrivateFloatFromGenericSkill(skill, "rechargeStopwatch");
            if (newFinalRechargeInterval == float.NaN) newFinalRechargeInterval = GetPrivateFloatFromGenericSkill(skill, "finalRechargeInterval");

            if (!skill.beginSkillCooldownOnSkillEnd || (skill.stateMachine.state.GetType() != skill.activationState.stateType))
            {
                rechargeStopwatchField.SetValue(skill, (float)rechargeStopwatchField.GetValue(skill) + dt);
            }
            if ((float)rechargeStopwatchField.GetValue(skill) >= (float)finalRechargeIntervalField.GetValue(skill))
            {
                restockSteplikeMethod.Invoke(skill, null);
            }
        }

        private void CheckCharacterMaster()
        {
            if (characterMaster == null)
            {
                GetSurvivorInfo();
            }
        }

        private void CheckSupport()
        {
            var find = Array.IndexOf<SurvivorIndex>(workingSurvivors, survivorIndex);
            var workingSurvivorString = string.Join(", ", workingSurvivors);
            if (find == (int)SurvivorIndex.None)
            {
                Chat.AddMessage($"This mod currently does not work with {survivorIndex}\nIt currently only works with {workingSurvivorString}");
                characterSupported = false;
            }
            else
            {
                Chat.AddMessage($"This mod currently does work with {survivorIndex}\nIt currently only works with {workingSurvivorString}");
                characterSupported = true;
            }
        }

        private void GetSurvivorInfo()
        {
            characterMaster = PlayerCharacterMasterController.instances[0].master;
            survivorIndex = GetSurvivorIndex(characterMaster);
        }

        private SurvivorIndex GetSurvivorIndex(CharacterMaster master)
        {
            var bodyPrefab = master.bodyPrefab;
            var def = SurvivorCatalog.FindSurvivorDefFromBody(bodyPrefab);
            var index = def.survivorIndex;
            
            return index;
        }

        public float GetSkillCooldown(GenericSkill skill)
        {
            float value = GetPrivateFloatFromGenericSkill(skill, "finalRechargeInterval");
            return value;
        }
        public float GetRechargeTimer(GenericSkill skill)
        {
            float value = GetPrivateFloatFromGenericSkill(skill, "rechargeStopwatch");
            return value;
        }

        public float GetPrivateFloatFromGenericSkill(GenericSkill skill, string field)
        {
            return (float)typeof(RoR2.GenericSkill).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(skill);
        }

        public void AlterCooldownByFlatAmount(GenericSkill skill, float amount)
        {
            AlterCooldown(skill, amount);
        }

        public void AlterCooldownByTotalPercentage(GenericSkill skill, float percent)
        {
            var amount = GetSkillCooldown(skill) * percent;
            AlterCooldown(skill, amount);
        }

        public void AlterCooldown(GenericSkill skill, float amount)
        {
            skill.RunRecharge(amount);
            //AlteredCooldownChatMessage(skill, amount);
        }

        private void AlteredCooldownChatMessage(GenericSkill skill, float amount)
        {
            var roundedNumber = decimal.Round((decimal)amount, 1, System.MidpointRounding.AwayFromZero);
            string line0 = "Skill: " + skill.skillName;
            string line1 = "Total cooldown: " + GetSkillCooldown(skill).ToString();
            string line2 = "Cooldown Reduction: " + roundedNumber;
            string line3 = "Remaining Cooldown: " + skill.cooldownRemaining;
            Chat.AddMessage(line0 + "\n" + line1 + "\n" + line2 + "\n" + line3);
        }

        private void ArrowOrb_OnArrival(On.RoR2.Orbs.ArrowOrb.orig_OnArrival orig, RoR2.Orbs.ArrowOrb self)
        {
            orig(self);

            if (SecondaryAbilityCooldownReductionOnHitAmount == 0) return;

            var skillLocator = self.attacker.GetComponent<SkillLocator>();
            var skill = skillLocator.secondary;
            AlterCooldownByFlatAmount(skill, SecondaryAbilityCooldownReductionOnHitAmount);
        }
        private void LightningOrb_OnArrival(On.RoR2.Orbs.LightningOrb.orig_OnArrival orig, RoR2.Orbs.LightningOrb self)
        {
            orig(self);

            if (SpecialAbilityCooldownReductionOnHitAmount == 0) return;

            if (self.lightningType == RoR2.Orbs.LightningOrb.LightningType.HuntressGlaive)
            {
                var skillLocator = self.attacker.GetComponent<SkillLocator>();
                var skill = skillLocator.special;

                Debug.Log(skill);

                AlterCooldownByFlatAmount(skill, SpecialAbilityCooldownReductionOnHitAmount);
            }
        }


        private static String SeeConfig {
            get => $"Current config is:\n" +
                $"Primary Skills recharging: { (PrimarySkillRechargingConfig.Value == true ? "Enabled" : "Disabled")}\n" +
                $"Secondary Skills recharging: { (SecondarySkillRechargingConfig.Value == true ? "Enabled" : "Disabled")}: {(SecondarySkillCooldownReductionOnHitAmountConfig.Value)}\n" +
                $"Utility Skills recharging: { (UtilitySkillRechargingConfig.Value == true ? "Enabled" : "Disabled")}\n" +
                $"Special Skills recharging: { (SpecialSkillRechargingConfig.Value == true ? "Enabled" : "Disabled")}: {(SpecialSkillCooldownReductionOnHitAmountConfig.Value)}\n" +
                $"Equipment Skills recharging: { (EquipmentRechargingConfig.Value == true ? "Enabled" : "Disabled")}\n";
        }


        private static void SetDefaultConfig()
        {
            PrimarySkillRechargingConfig.Value = true;
            SecondarySkillRechargingConfig.Value = false;
            UtilitySkillRechargingConfig.Value = true;
            SpecialSkillRechargingConfig.Value = false;
            EquipmentRechargingConfig.Value = true;

            SecondarySkillCooldownReductionOnHitAmountConfig.Value = 1;
            SpecialSkillCooldownReductionOnHitAmountConfig.Value = 3f;
        }



        [ConCommand(commandName = "COH_Primary", flags = ConVarFlags.None, helpText = "Primary Skill configurations.")]
        private static void CCPrimary(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Debug.Log($"Primary Skills recharging: { (PrimarySkillRechargingConfig.Value == true ? "Enabled" : "Disabled")}");
            }
            else if (args.Count == 1)
            {
                if (!bool.TryParse(args[0], out var recharging))
                {
                    Debug.Log("First argument was invalid. It should be a boolean (True / False)");
                    return;
                }

                PrimarySkillRechargingConfig.Value = recharging;

                Debug.Log($"Primary Skills recharging: { (PrimarySkillRechargingConfig.Value == true ? "Enabled" : "Disabled")}");
            }
            else
            {
                Debug.Log("One arguments expected. A boolean (true or False)");
            }
        }

        [ConCommand(commandName = "COH_Secondary", flags = ConVarFlags.None, helpText = "Secondary Skill configurations.")]
        private static void CCSecondary(ConCommandArgs args)
        {

            if (args.Count == 0)
            {
                Debug.Log($"Primary Skills recharging: { (SecondarySkillRechargingConfig.Value == true ? "Enabled" : "Disabled")}");
            }
            else if (args.Count == 2)
            {
                if (!bool.TryParse(args[0], out var recharging))
                {
                    Debug.Log("First argument was invalid. It should be a boolean (True / False)");
                    return;
                }
                if (!float.TryParse(args[1], out var amount))
                {
                    Debug.Log("Second argument was invalid. It should be a positive float (any number 0 or above)");
                    return;
                }

                SecondarySkillRechargingConfig.Value = recharging;
                SecondarySkillCooldownReductionOnHitAmountConfig.Value = amount;


                Debug.Log($"Primary Skills recharging: { (SecondarySkillRechargingConfig.Value == true ? "Enabled" : "Disabled")}: {SecondarySkillCooldownReductionOnHitAmountConfig.Value} seconds");
            }
            else
            {
                Debug.Log("Two arguments expected. A boolean (true or False), and a positive float (any number 0 or higher)");
            }
        }

        [ConCommand(commandName = "COH_Utility", flags = ConVarFlags.None, helpText = "Utility Skill configurations.")]
        private static void CCUtility(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Debug.Log($"Utility Skills recharging: { (UtilitySkillRechargingConfig.Value == true ? "Enabled" : "Disabled")}");
            }
            else if (args.Count == 1)
            {
                if (!bool.TryParse(args[0], out var recharging))
                {
                    Debug.Log("First argument was invalid. It should be a boolean (True / False)");
                    return;
                }


                UtilitySkillRechargingConfig.Value = recharging;

                Debug.Log($"Utility Skills recharging: { (UtilitySkillRechargingConfig.Value == true ? "Enabled" : "Disabled")}");
            }
            else
            {
                Debug.Log("One arguments expected. A boolean (true or False)");
            }
        }

        [ConCommand(commandName = "COH_Special", flags = ConVarFlags.None, helpText = "Sets Special Skill configurations.")]
        private static void CCSpecial(ConCommandArgs args)
        {

            if (args.Count == 0)
            {
                Debug.Log($"Special Skills recharging: { (SpecialSkillRechargingConfig.Value == true ? "Enabled" : "Disabled")}");
            }
            else if (args.Count == 2)
            {
                if (!bool.TryParse(args[0], out var recharging))
                {
                    Debug.Log("First argument was invalid. It should be a boolean (True / False)");
                    return;
                }
                if (!float.TryParse(args[1], out var amount))
                {
                    Debug.Log("Second argument was invalid. It should be a positive float (any number 0 or above)");
                    return;
                }

                SpecialSkillRechargingConfig.Value = recharging;
                SpecialSkillCooldownReductionOnHitAmountConfig.Value = amount;

                Debug.Log($"Special Skills recharging: { (SpecialSkillRechargingConfig.Value == true ? "Enabled" : "Disabled")}: {SpecialSkillCooldownReductionOnHitAmountConfig.Value} seconds");
            }
            else
            {
                Debug.Log("Two arguments expected. A boolean (true or False), and a positive float (any number 0 or higher)");
            }
        }

        [ConCommand(commandName = "COH_Equipment", flags = ConVarFlags.None, helpText = "Sets Equipment Skill configurations.")]
        private static void CCSetEquipment(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Debug.Log($"Equipment Skills recharging: { (EquipmentRechargingConfig.Value == true ? "Enabled" : "Disabled")}");
            }
            else if (args.Count == 1)
            {
                if (!bool.TryParse(args[0], out var recharging))
                {
                    Debug.Log("First argument was invalid. It should be a boolean (True / False)");
                }

                PrimarySkillRechargingConfig.Value = recharging;

                Debug.Log($"Equipment Skills recharging: { (UtilitySkillRechargingConfig.Value == true ? "Enabled" : "Disabled")}");
            }
            else
            {
                Debug.Log("One arguments expected. A boolean (true or False)");
            }
        }

        [ConCommand(commandName = "COH_GetConfig", flags = ConVarFlags.None, helpText = "Displays the current configuration.")]
        private static void CCGetConfig(ConCommandArgs args)
        {
            Debug.Log(args.Count != 0
                ? "Does not accept arguments. Did you mean something else?"
                : SeeConfig);
        }

        [ConCommand(commandName = "COH_ResetConfig", flags = ConVarFlags.None, helpText = "Sets the config back to its default state")]
        private static void CCResetConfig(ConCommandArgs args)
        {
            SetDefaultConfig();
            Debug.Log(SeeConfig);
        }
    }
}