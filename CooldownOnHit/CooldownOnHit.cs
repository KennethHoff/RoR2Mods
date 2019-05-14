using BepInEx;
using BepInEx.Configuration;
using MiniRpcLib;
using MiniRpcLib.Func;
using MiniRpcLib.Action;
using RoR2;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using Utilities;

namespace CooldownOnHit
{
    [BepInDependency("com.bepis.r2api")]
    [BepInDependency(MiniRpcPlugin.Dependency)]
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class CooldownOnHit : BaseUnityPlugin
    {
        private const string ModGuid = "com.Modernkennnern.CooldownOnHit";
        private const string ModName = "CooldownOnHit";
        private const string ModVersion = "0.2.0";
        // TODO: Redo the way cooldowns are displayed.
        // Currently, Huntress Glaive has 7second cooldown - it says 7 until you've hit enough times to get it to 6.
        // What I would like is to turn that number into a "Hits remaining" visual that, instead of telling you a (non-functioning) timer, tells you how many hits until the cooldown is off.
        // This would require a function that turns the total cooldown into a number (7 seconds = 10 hits, for example), and ideally would be different for each character. (MUL-T would be bonkers with only 10 hits, while Sniper would be incredibly bad)

        // TODO: Turn this into a (Lunar) item

        // TODO: Allow the possibility of assigning specific skillslots to other damage sources

        // TODO: Change cooldown reduction to scale with ProcCoeffiency (This is on the damage source, so it will be somewhat awkward to implement)

        // The ideal method for implementing would be: You have two parameters. The first is a GenericSkill that, when it hits an enemy, will reduce the cooldown of the second GenericSkill.

        // TODO: Change the hook from GenericSkill.RunRecharge => GenericSkill.FixedUpdate

        // TODO: (LOW PRIORITY) Enable OnDisable support



        // Get Local User with: LocalUserManager.GetFIrstLocalUser(), or NetworkUser.inputPlayer, or using the Camera Rig Controller (Not sure about the specifics - I will probably found it if I look





        public static int PrimarySkillRecharging {
            get => savedConfig.primaryRecharging;
            protected set => savedConfig.SetPrimaryRecharging(value == 1 ? true : false);
        }
        public static float PrimarySkillCooldownReductionOnHitAmount {
            get => savedConfig.primaryAmount;
            protected set => savedConfig.SetPrimaryAmount(value);
        }


        public static int SecondarySkillRecharging {
            get => savedConfig.secondaryRecharging;
            protected set => savedConfig.SetSecondaryRecharging(value == 1 ? true : false);
        }
        public static float SecondarySkillCooldownReductionOnHitAmount {
            get => savedConfig.secondaryAmount;
            protected set => savedConfig.SetSecondaryAmount(value);
        }


        public static int UtilitySkillRecharging {
            get => savedConfig.utilityRecharging;
            protected set => savedConfig.SetUtilityRecharging(value == 1 ? true : false);
        }
        public static float UtilitySkillCooldownReductionOnHitAmount {
            get => savedConfig.utilityAmount;
            protected set => savedConfig.SetUtilityAmount(value);
        }


        public static int SpecialSkillRecharging {
            get => savedConfig.specialRecharging;
            protected set => savedConfig.SetSpecialRecharging(value == 1 ? true : false);
        }
        public static float SpecialSkillCooldownReductionOnHitAmount {
            get => savedConfig.specialAmount;
            protected set => savedConfig.SetSpecialAmount(value);
        }


        public static int EquipmentRecharging {
            get => savedConfig.equipmentRecharging;
            protected set => savedConfig.SetEquipmentRecharging(value == 1 ? true : false);
        }
        public static float EquipmentCooldownReductionOnHitAmount {
            get => savedConfig.equipmentAmount;
            protected set => savedConfig.SetEquipmentAmount(value);
        }


        public IRpcAction<ConfigContainer> ConfigRequestClient { get; set; }

        // (int)SurvivorIndex => SurvivorIndex(int)
        public IRpcFunc<int, bool> CharacterSupportedRequestHost { get; set; }


        public readonly SurvivorIndex[] workingSurvivors = new SurvivorIndex[] { SurvivorIndex.Huntress };


        private float newRechargeStopwatch;
        private float newFinalRechargeInterval;



        public bool checkedForLocalInfo;


        private bool localPlayercharacterSupported;
        private int localPlayerNumber;

        private CharacterMaster localPlayerCharacterMaster;
        private SurvivorIndex localPlayerSurvivorIndex;


        public SkillLocator localPlayerSkillLoc;
        public GenericSkill LocalPlayerPrimarySkill {
            get => localPlayerSkillLoc.GetSkill(SkillSlot.Primary);
        }
        public GenericSkill LocalPlayerSecondarySkill {
            get => localPlayerSkillLoc.GetSkill(SkillSlot.Secondary);
        }
        public GenericSkill LocalPlayerUtilitySkill {
            get => localPlayerSkillLoc.GetSkill(SkillSlot.Utility);
        }
        public GenericSkill LocalPlayerSpecialSkill {
            get => localPlayerSkillLoc.GetSkill(SkillSlot.Special);
        }


        public void Awake()
        {

            RegisterMiniRpc();
            EnableEvents_Awake();

        }

        public void EnableEvents_PodDescent()
        {
            Debug.LogWarning("Survivor Pod Descends");


            localPlayerCharacterMaster = LocalUserManager.GetFirstLocalUser().cachedMasterController.master;


            LocalUserManager.GetFirstLocalUser().onBodyChanged += CooldownOnHit_onBodyChanged;


            SetDefaultValues();
            SendConfigToPlayers();
            GetLocalPlayerInfo();

            if (currentRunConfig == null)
            {
                Debug.LogWarning("Current Run Config is null. RunRecharge will not... Run >:)");
            }




            foreach (var masterController in PlayerCharacterMasterController.instances)
            {
                Debug.Log(GetSurvivorIndex(masterController.master));
                //redo it so it works based off of this instead of each local user asking
            }

        }

        private void CooldownOnHit_onBodyChanged()
        {
            CheckLocalCharacterSupported();
        }

        public void SetDefaultValues()
        {

            checkedForLocalInfo = false;
            newRechargeStopwatch = float.NaN;
            newFinalRechargeInterval = float.NaN;
    }

        private void GetConfig()
        {
            Debug.Log("Loading Config");
            savedConfig = LoadConfigFromFile();
            Debug.Log("Config loaded\n" + savedConfig.ToString());
        }

        private void SendConfigToPlayers()
        {
            //if(NetworkServer.active)
            //{
            ConfigContainer temp = savedConfig;
            ConfigRequestClient.Invoke(temp);
            //}
        }

        public void EnableEvents_Awake()
        {
            WrapConfig();

            GetConfig();

            On.RoR2.Console.Awake += (orig, self) =>
            {
                orig(self);
                CommandHelper.RegisterCommands(self);
            };

            On.EntityStates.SurvivorPod.Descent.OnEnter += (orig, self) =>
            {
                orig(self);
                EnableEvents_PodDescent();
            };

            On.EntityStates.SurvivorPod.Landed.OnEnter += (orig, self) =>
            {
                orig(self);
                EnableEvents_PodLanded();
            };


            // Huntress
            On.RoR2.Orbs.LightningOrb.OnArrival += LightningOrb_OnArrival;
            On.RoR2.Orbs.ArrowOrb.OnArrival += ArrowOrb_OnArrival;

            //// Commando
            //On.RoR2.BulletAttack.DefaultHitCallback += BulletAttack_DefaultHitCallback;

        }

        private void EnableEvents_PodLanded()
        {
            On.RoR2.GenericSkill.RunRecharge += GenericSkill_RunRecharge;
        }

        private void RegisterMiniRpc()
        {

            var miniRpc = MiniRpc.CreateInstance(ModGuid);

            ConfigRequestClient = miniRpc.RegisterAction(Target.Client, (NetworkUser user, ConfigContainer configContainer ) =>
            {
                currentRunConfig = configContainer;
                Debug.Log($"[Client]: Host sent us {configContainer.ToString()})");
            });

            CharacterSupportedRequestHost = miniRpc.RegisterFunc<int, bool>(Target.Server, (NetworkUser user, int i ) =>
            {
                var index = (SurvivorIndex)i;
                var supported = CheckCharacterSupport(index);
                return supported;
            });

        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                if (!checkedForLocalInfo)
                {
                    GetLocalPlayerInfo();
                    checkedForLocalInfo = true;
                }

                CheckLocalCharacterSupported();


                var stringerino = "Pressed F2";
                if (NetworkServer.active)
                {
                    stringerino += " on server";
                }
                else
                {
                    stringerino += " on client";
                }
                Chat.AddMessage(stringerino);


                Chat.AddMessage("Character supported? " + localPlayercharacterSupported.ToString());
            }
        }

        public void GetLocalPlayerInfo()
        {


            Debug.Log("Starting getting local player info");
            for (int i = 0; i < PlayerCharacterMasterController.instances.Count; i++)
            {
            Debug.Log("Getting local player info for player: " + i);
                if (PlayerCharacterMasterController.instances[i].master == localPlayerCharacterMaster)
                {
                    Debug.Log("You are player: " + i);
                    localPlayerNumber = i;
                    localPlayerCharacterMaster = PlayerCharacterMasterController.instances[i].master;
                    Debug.LogWarning("Character master: " + localPlayerCharacterMaster);
                    localPlayerSkillLoc = GetSkillLoc(localPlayerCharacterMaster);
                    Debug.LogWarning("Skill Locator: " + localPlayerSkillLoc);
                    localPlayerSurvivorIndex = GetSurvivorIndex(localPlayerCharacterMaster);
                    Debug.LogWarning("Survivor Index: " + localPlayerSurvivorIndex);
                    Chat_GetLocalPlayerInfo();
                }
            }

            Chat_GetLocalPlayerInfo();

        }

        public void Chat_GetLocalPlayerInfo()
        {
            Chat.AddMessage("You are player number: " + localPlayerNumber + ", and you are playing the Survivor: " + localPlayerSurvivorIndex);
        }



        private void ReduceCooldown(GenericSkill skill, float amount)
        {
            AlterSkillCooldown(skill, amount);
        }

        private float GetCooldownReduction(SkillLocator locator, GenericSkill skill)
        {
            switch (locator.FindSkillSlot(skill))
            {
                case SkillSlot.Primary:
                    return currentRunConfig.primaryAmount;

                case SkillSlot.Secondary:
                    return currentRunConfig.secondaryAmount;

                case SkillSlot.Utility:
                    return currentRunConfig.utilityAmount;

                case SkillSlot.Special:
                    return currentRunConfig.specialAmount;

                case SkillSlot.None:
                    Debug.LogWarning("Skill is using SkillSlot None");
                    return 0.0f;

                default:
                    Debug.LogWarning("Not a skillSlot");
                    return 0.0f;
            }
        }

        public static SkillLocator GetSkillLoc(CharacterMaster master)
        {
            return master.GetBody().GetComponent<SkillLocator>();
        }




        public bool BulletAttack_DefaultHitCallback(On.RoR2.BulletAttack.orig_DefaultHitCallback orig, BulletAttack self, ref BulletAttack.BulletHit hitInfo)
        {
            var b = orig(self, ref hitInfo);

            if (!hitInfo.entityObject.GetComponent<TeamComponent>())
            {
                return b;
            }

            if (hitInfo.entityObject.GetComponent<TeamComponent>().teamIndex == TeamIndex.Monster)
            {
                Debug.Log("Hit a monster!");
            }

            return b;
        }

        public bool CanAffectCooldown(GenericSkill skill)
        {
            if (
                (skill == LocalPlayerPrimarySkill && currentRunConfig.primaryAmount == 0) ||
                (skill == LocalPlayerSecondarySkill && currentRunConfig.secondaryAmount == 0) ||
                (skill == LocalPlayerUtilitySkill && currentRunConfig.utilityAmount == 0) ||
                (skill == LocalPlayerSpecialSkill && currentRunConfig.specialAmount == 0))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        


        // Client tells the server when he wants to recharge. Server responds with a the amount of cooldown to reduce the skill by.
        public void GenericSkill_RunRecharge(On.RoR2.GenericSkill.orig_RunRecharge orig, GenericSkill skill, float dt)
            {
                if (CanRecharge(skill))
                {
                    orig(skill, dt);
                }

                if (!CanAffectCooldown(skill)) return;

                if (!localPlayercharacterSupported) return;

                if (skill.stock >= skill.maxStock) return;

                // Not entirely sure about this next line..
                var dt2 = Time.fixedDeltaTime;
                if (dt == dt2) return;

                ReduceCooldown(skill, dt);

            }
            public void AlterSkillCooldown(GenericSkill skill, float amount)
        {
            var skillType = skill.GetType();

            var rechargeStopwatchField = skillType.GetField("rechargeStopwatch", BindingFlags.NonPublic | BindingFlags.Instance);
            var finalRechargeIntervalField = skillType.GetField("finalRechargeInterval", BindingFlags.NonPublic | BindingFlags.Instance);
            var restockSteplikeMethod = skillType.GetMethod("RestockSteplike", BindingFlags.NonPublic | BindingFlags.Instance);

            if (newRechargeStopwatch == float.NaN) newRechargeStopwatch = GetPrivateFloatFromGenericSkillReflection(skill, "rechargeStopwatch");
            if (newFinalRechargeInterval == float.NaN) newFinalRechargeInterval = GetPrivateFloatFromGenericSkillReflection(skill, "finalRechargeInterval");

            if (!skill.beginSkillCooldownOnSkillEnd || (skill.stateMachine.state.GetType() != skill.activationState.stateType))
            {
                rechargeStopwatchField.SetValue(skill, (float)rechargeStopwatchField.GetValue(skill) + amount);
            }
            if ((float)rechargeStopwatchField.GetValue(skill) >= (float)finalRechargeIntervalField.GetValue(skill))
            {
                restockSteplikeMethod.Invoke(skill, null);
            }
        }

        private void CheckLocalCharacterSupported()
        {
            CharacterSupportedRequestHost.Invoke((int)localPlayerSurvivorIndex, result =>
            {
                Debug.Log(localPlayerSurvivorIndex.ToString());
                localPlayercharacterSupported = result;

                Debug.Log($"[Client]: Server sent me {result} on my request on CharacterSupport");
            });
        }

        public bool CheckCharacterSupport(SurvivorIndex index)
        {
            var find = Array.IndexOf<SurvivorIndex>(workingSurvivors, index);
            var workingSurvivorsString = string.Join(", ", workingSurvivors);

            return (find == (int)SurvivorIndex.None);

        }

        public SurvivorIndex GetSurvivorIndex(CharacterMaster master)
        {
            var bodyPrefab = master.bodyPrefab;
            var def = SurvivorCatalog.FindSurvivorDefFromBody(bodyPrefab);
            var index = def.survivorIndex;

            return index;
        }

        public float GetSkillCooldownReflection(GenericSkill skill)
        {
            float value = GetPrivateFloatFromGenericSkillReflection(skill, "finalRechargeInterval");
            return value;
        }
        public float GetRechargeTimerReflection(GenericSkill skill)
        {
            float value = GetPrivateFloatFromGenericSkillReflection(skill, "rechargeStopwatch");
            return value;
        }

        public float GetPrivateFloatFromGenericSkillReflection(GenericSkill skill, string field)
        {
            return (float)typeof(RoR2.GenericSkill).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(skill);
        }

        public void AlterCooldownByFlatAmount(GenericSkill skill, float amount)
        {
            AlterCooldown(skill, amount);
        }

        public void AlterCooldownByTotalPercentage(GenericSkill skill, float percent)
        {
            var amount = GetSkillCooldownReflection(skill) * percent;
            AlterCooldown(skill, amount);
        }

        public void AlterCooldown(GenericSkill skill, float amount)
        {
            skill.RunRecharge(amount);
        }

        private void AlteredCooldownChatMessage(GenericSkill skill, float amount)
        {
            var roundedNumber = decimal.Round((decimal)amount, 1, System.MidpointRounding.AwayFromZero);
            string line0 = "Skill: " + skill.skillName;
            string line1 = "Total cooldown: " + GetSkillCooldownReflection(skill).ToString();
            string line2 = "Cooldown Reduction: " + roundedNumber;
            string line3 = "Remaining Cooldown: " + skill.cooldownRemaining;
            Chat.AddMessage(line0 + "\n" + line1 + "\n" + line2 + "\n" + line3);
        }

        private void ArrowOrb_OnArrival(On.RoR2.Orbs.ArrowOrb.orig_OnArrival orig, RoR2.Orbs.ArrowOrb self)
        {
            orig(self);

            if (currentRunConfig.secondaryAmount == 0) return;

            var skillLocator = self.attacker.GetComponent<SkillLocator>();
            var skill = skillLocator.secondary;
            AlterCooldownByFlatAmount(skill, currentRunConfig.secondaryAmount);
        }
        private void LightningOrb_OnArrival(On.RoR2.Orbs.LightningOrb.orig_OnArrival orig, RoR2.Orbs.LightningOrb self)
        {
            orig(self);

            if (currentRunConfig.specialAmount == 0) return;

            if (self.lightningType == RoR2.Orbs.LightningOrb.LightningType.HuntressGlaive)
            {
                var skillLocator = self.attacker.GetComponent<SkillLocator>();
                var skill = skillLocator.special;

                Debug.Log(skill);

                AlterCooldownByFlatAmount(skill, currentRunConfig.specialAmount);
            }
        }
        private bool CanRecharge(GenericSkill skill)
        {
            if (
                (skill == LocalPlayerPrimarySkill && currentRunConfig.primaryRecharging == 1) ||
                (skill == LocalPlayerSecondarySkill && currentRunConfig.secondaryRecharging == 1) ||
                (skill == LocalPlayerUtilitySkill && currentRunConfig.utilityRecharging == 1) ||
                (skill == LocalPlayerSpecialSkill && currentRunConfig.specialRecharging == 1))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        [ConCommand(commandName = "COH_Primary", flags = ConVarFlags.None, helpText = "Primary Skill configurations.")]
        private static void CCPrimary(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Debug.Log(savedConfig.GetPrimary);
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

                savedConfig.SetPrimary(recharging, amount);
                SaveConfigToFile(savedConfig);

                Debug.Log(savedConfig.GetPrimary);
            }
            else
            {
                Debug.Log("Two arguments expected. A boolean (true or False), and a positive float (any number 0 or higher)");
            }
        }

        [ConCommand(commandName = "COH_Secondary", flags = ConVarFlags.None, helpText = "Secondary Skill configurations.")]
        private static void CCSecondary(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Debug.Log(savedConfig.GetSecondary);
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

                savedConfig.SetSecondary(recharging, amount);
                SaveConfigToFile(savedConfig);

                Debug.Log(savedConfig.GetSecondary);
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
                Debug.Log(savedConfig.GetUtility);
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

                savedConfig.SetUtility(recharging, amount);
                SaveConfigToFile(savedConfig);

                Debug.Log(savedConfig.GetUtility);
            }
            else
            {
                Debug.Log("Two arguments expected. A boolean (true or False), and a positive float (any number 0 or higher)");
            }
            Debug.Log("One arguments expected. A boolean (true or False)");
        }

        [ConCommand(commandName = "COH_Special", flags = ConVarFlags.None, helpText = "Sets Special Skill configurations.")]
        private static void CCSpecial(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Debug.Log(savedConfig.GetSpecial);
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

                savedConfig.SetSpecial(recharging, amount);
                SaveConfigToFile(savedConfig);

                Debug.Log(savedConfig.GetSpecial);
            }
            else
            {
                Debug.Log("Two arguments expected. A boolean (true or False), and a positive float (any number 0 or higher)");
            }
            Debug.Log("One arguments expected. A boolean (true or False)");
        }

        [ConCommand(commandName = "COH_Equipment", flags = ConVarFlags.None, helpText = "Sets Equipment Skill configurations.")]
        private static void CCSetEquipment(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Debug.Log(savedConfig.GetEquipment);
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

                savedConfig.SetEquipment(recharging, amount);
                SaveConfigToFile(savedConfig);

                Debug.Log(savedConfig.GetEquipment);
            }
            else
            {
                Debug.Log("Two arguments expected. A boolean (true or False), and a positive float (any number 0 or higher)");
            }
            Debug.Log("One arguments expected. A boolean (true or False)");
        }

        [ConCommand(commandName = "COH_GetConfig", flags = ConVarFlags.None, helpText = "Displays the current configuration.")]
        private static void CCGetConfig(ConCommandArgs args)
        {
            Debug.Log(args.Count == 0  ? savedConfig.ToString() : "Does not accept arguments. Did you mean something else?");
        }

        [ConCommand(commandName = "COH_ResetConfig", flags = ConVarFlags.None, helpText = "Sets the config back to its default state")]
        private static void CCResetConfig(ConCommandArgs args)
        {
            savedConfig.SetDefault();
            Debug.Log(savedConfig.ToString());
        }




        public static ConfigContainer savedConfig = new ConfigContainer();

        public ConfigContainer currentRunConfig;

        private static ConfigWrapper<bool> PrimarySkillRechargingConfig { get; set; }
        private static ConfigWrapper<string> PrimarySkillCooldownReductionOnHitAmountConfig { get; set; }


        private static ConfigWrapper<bool> SecondarySkillRechargingConfig { get; set; }
        private static ConfigWrapper<string> SecondarySkillCooldownReductionOnHitAmountConfig { get; set; }


        private static ConfigWrapper<bool> UtilitySkillRechargingConfig { get; set; }
        private static ConfigWrapper<string> UtilitySkillCooldownReductionOnHitAmountConfig { get; set; }


        private static ConfigWrapper<bool> SpecialSkillRechargingConfig { get; set; }
        private static ConfigWrapper<string> SpecialSkillCooldownReductionOnHitAmountConfig { get; set; }


        private static ConfigWrapper<bool> EquipmentRechargingConfig { get; set; }
        private static ConfigWrapper<string> EquipmentCooldownReductionOnHitAmountConfig { get; set; }


        public void WrapConfig()
        {
            var WIPDescription = "Does not do anything. I'll gladly accept suggestions";
            var WIPvalue = @"¯\_(ツ)_/¯";

            PrimarySkillRechargingConfig = Config.Wrap(
                "Cooldowns",
                "PrimarySkillRecharging",
                "Enables normal recharging of primary skill",
                true);
            PrimarySkillCooldownReductionOnHitAmountConfig = Config.Wrap(
                "Cooldowns",
                "PrimarySkillCooldownReductionOnHitAmount",
                WIPDescription,
                WIPvalue);


            SecondarySkillRechargingConfig = Config.Wrap(
                "Cooldowns",
                "SecondarySkillRecharging",
                "Enables normal recharging of Secondary Skill",
                false);
            SecondarySkillCooldownReductionOnHitAmountConfig = Config.Wrap(
                "Cooldowns",
                "SecondarySkillCooldownReductionOnHitAmount",
                "How many seconds ot reduce the Secondary Skill (RMB) cooldown by on each hit with the Primary Skill",
                "1");


            UtilitySkillRechargingConfig = Config.Wrap(
                "Cooldowns",
                "UtilitySkillRecharging",
                "Enables normal recharging of Utility skills",
                true);
            UtilitySkillCooldownReductionOnHitAmountConfig = Config.Wrap(
                "Cooldowns",
                "UtilitySkillCooldownReductionOnHitAmount",
                WIPDescription,
                WIPvalue);


            SpecialSkillRechargingConfig = Config.Wrap(
                "Cooldowns",
                "SpecialSkillRecharging",
                "Enables normal recharging of Special Skills",
                false);
            SpecialSkillCooldownReductionOnHitAmountConfig = Config.Wrap(
                "Cooldowns",
                "SpecialAbilityCooldownReductionOnHitAmount",
                "How many seconds to reduce the Special Skill(R) cooldown by on each hit with the Secondary skill.",
                "2.5");


            EquipmentRechargingConfig = Config.Wrap(
                "Cooldowns",
                "EquipmentRecharging",
                "W.I.P - Cannot currently be disabled with this mod (Coming in a future major update)\nEnables normal recharging of Equipment",
                true);
            EquipmentCooldownReductionOnHitAmountConfig = Config.Wrap(
                "Cooldowns",
                "EquipmentCooldownReductionOnHitAmount",
                WIPDescription,
                WIPvalue);
        }

        public static void SaveConfigToFile(ConfigContainer container)
        {
            PrimarySkillRechargingConfig.Value = container.primaryRecharging == 1 ? true : false;
            PrimarySkillCooldownReductionOnHitAmountConfig.Value = container.primaryAmount.ToString();

            SecondarySkillRechargingConfig.Value = container.secondaryRecharging == 1 ? true : false;
            SecondarySkillCooldownReductionOnHitAmountConfig.Value = container.secondaryAmount.ToString();

            UtilitySkillRechargingConfig.Value = container.utilityRecharging == 1 ? true : false;
            UtilitySkillCooldownReductionOnHitAmountConfig.Value = container.utilityAmount.ToString();

            SpecialSkillRechargingConfig.Value = container.specialRecharging == 1 ? true : false;
            SpecialSkillCooldownReductionOnHitAmountConfig.Value = container.specialAmount.ToString();

            EquipmentRechargingConfig.Value = container.equipmentRecharging == 1 ? true : false;
            EquipmentCooldownReductionOnHitAmountConfig.Value = container.equipmentAmount.ToString();
        }
        public static ConfigContainer LoadConfigFromFile()
        {
            ConfigContainer tempContainer = new ConfigContainer();
            tempContainer.SetPrimary(  PrimarySkillRechargingConfig.Value,     ConfigHelper.ConfigToFloat(PrimarySkillCooldownReductionOnHitAmountConfig.Value));
            tempContainer.SetSecondary(SecondarySkillRechargingConfig.Value,   ConfigHelper.ConfigToFloat(SecondarySkillCooldownReductionOnHitAmountConfig.Value));
            tempContainer.SetUtility(  UtilitySkillRechargingConfig.Value,     ConfigHelper.ConfigToFloat(UtilitySkillCooldownReductionOnHitAmountConfig.Value));
            tempContainer.SetSpecial(  SpecialSkillRechargingConfig.Value,     ConfigHelper.ConfigToFloat(SpecialSkillCooldownReductionOnHitAmountConfig.Value));
            tempContainer.SetEquipment(EquipmentRechargingConfig.Value,        ConfigHelper.ConfigToFloat(EquipmentCooldownReductionOnHitAmountConfig.Value));
            return tempContainer;
        }
    }
}