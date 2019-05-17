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



        public static bool PrimarySkillRecharging {
            get => savedConfig.primaryRecharging;
            protected set => savedConfig.SetPrimaryRecharging(value);
        }
        public static float PrimarySkillCooldownReductionOnHitAmount {
            get => savedConfig.primaryAmount;
            protected set => savedConfig.SetPrimaryAmount(value);
        }


        public static bool SecondarySkillRecharging {
            get => savedConfig.secondaryRecharging;
            protected set => savedConfig.SetSecondaryRecharging(value);
        }
        public static float SecondarySkillCooldownReductionOnHitAmount {
            get => savedConfig.secondaryAmount;
            protected set => savedConfig.SetSecondaryAmount(value);
        }


        public static bool UtilitySkillRecharging {
            get => savedConfig.utilityRecharging;
            protected set => savedConfig.SetUtilityRecharging(value);
        }
        public static float UtilitySkillCooldownReductionOnHitAmount {
            get => savedConfig.utilityAmount;
            protected set => savedConfig.SetUtilityAmount(value);
        }


        public static bool SpecialSkillRecharging {
            get => savedConfig.specialRecharging;
            protected set => savedConfig.SetSpecialRecharging(value);
        }
        public static float SpecialSkillCooldownReductionOnHitAmount {
            get => savedConfig.specialAmount;
            protected set => savedConfig.SetSpecialAmount(value);
        }


        public static bool EquipmentRecharging {
            get => savedConfig.equipmentRecharging;
            protected set => savedConfig.SetEquipmentRecharging(value);
        }
        public static float EquipmentCooldownReductionOnHitAmount {
            get => savedConfig.equipmentAmount;
            protected set => savedConfig.SetEquipmentAmount(value);
        }


        public IRpcAction<ConfigContainer> ConfigRequestClient { get; set; }

        // (int)SurvivorIndex => SurvivorIndex(int)
        //public IRpcFunc<int, bool> CharacterSupportedRequestHost { get; set; }

        public IRpcFunc<int, CharacterSupportContainer> CharacterSupportedRequestHost { get; set; }

        public IRpcFunc<int, int> SurvivorIndexRequestHost { get; set; }

        public IRpcAction<int> CooldownRequestClient { get; set; }

        public static bool DebugActive = false;

        private bool runstarted = false;




        public readonly SurvivorIndex[] workingSurvivors = new SurvivorIndex[] { SurvivorIndex.Huntress };


        private float newRechargeStopwatch;
        private float newFinalRechargeInterval;



        private bool localPlayercharacterSupported;

        private CharacterMaster localPlayerCharacterMaster;
        private SurvivorIndex localPlayerSurvivorIndex;


        public SkillLocator localPlayerSkillLoc;

        private GenericSkill localPlayerPrimarySkill;
        private GenericSkill localPlayerSecondarySkill;
        private GenericSkill localPlayerUtilitySkill;
        private GenericSkill localPlayerSpecialSkill;

        public void Awake()
        {
            RegisterMiniRpc();
            EnableEvents_Awake();

        }

        public void SetDefaultValues()
        {

            newRechargeStopwatch = float.NaN;
            newFinalRechargeInterval = float.NaN;
    }

        private void GetConfig()
        {
            if (DebugActive) Debug.Log("Loading Config");
            savedConfig = LoadConfigFromFile();
            if (DebugActive) Debug.Log("Config loaded\n" + savedConfig.ToString());
        }

        private void SendConfigToPlayers()
        {
            if (NetworkServer.active)
            {
                ConfigRequestClient.Invoke(savedConfig);
            }
        }

        public void EnableEvents_Awake()
        {
            WrapConfig();

            GetConfig();

            On.RoR2.GenericSkill.RunRecharge += GenericSkill_RunRecharge;

            On.RoR2.Console.Awake += (orig, self) =>
            {
                orig(self);
                CommandHelper.RegisterCommands(self);
            };

            On.EntityStates.SurvivorPod.Descent.OnEnter += (orig, self) =>
            {
                orig(self);
                StartRun();
            };

            On.EntityStates.SurvivorPod.Landed.OnEnter += (orig, self) =>
            {
                orig(self);
                StartStage();
            };


            On.RoR2.Stage.RespawnCharacter += (orig, self, CharacterMaster) =>
            {
                orig(self, CharacterMaster);
                if (!runstarted) return;
                if (DebugActive) Debug.Log("RoR2.Stage.RespawnCharacter");
                StartStage();
            };


            // Huntress
            On.RoR2.Orbs.LightningOrb.OnArrival += LightningOrb_OnArrival;
            On.RoR2.Orbs.ArrowOrb.OnArrival += ArrowOrb_OnArrival;

            //// Commando
            //On.RoR2.BulletAttack.DefaultHitCallback += BulletAttack_DefaultHitCallback;

        }



        public void StartRun()
        {
            SetDefaultValues();
            SendConfigToPlayers();
            runstarted = true;
            //GetLocalPlayerInfo();
            //CheckLocalCharacterSupported();
        }


        private void StartStage()
        {
            GetLocalPlayerInfo();
            CheckLocalCharacterSupported();
        }

        private void RegisterMiniRpc()
        {

            var miniRpc = MiniRpc.CreateInstance(ModGuid);

            ConfigRequestClient = miniRpc.RegisterAction(Target.Client, (NetworkUser user, ConfigContainer configContainer ) =>
            {
                if (DebugActive) Debug.Log("Saved config:\n" + savedConfig.ToString());
                currentRunConfig = configContainer;
                if (DebugActive) Debug.Log("Current config:\n" + currentRunConfig.ToString());
            });

            CharacterSupportedRequestHost = miniRpc.RegisterFunc<int, CharacterSupportContainer>(Target.Server, (NetworkUser user, int i) =>
            {
                CharacterSupportContainer ret = new CharacterSupportContainer();
                var index = GetSurvivorIndex(user.master);
                var supported = CheckCharacterSupport(index);

                ret.index = (int)index;
                ret.supported = supported;


                if (DebugActive) Debug.Log($"[Host]: Client {user} sent me {index}. It is {(supported ? "" : "not")} supported");
                return ret;
            });

            SurvivorIndexRequestHost = miniRpc.RegisterFunc<int, int>(Target.Server, (NetworkUser user, int i) =>
            {
                var index = GetSurvivorIndex(user.master);
                return (int)index;
            });

            CooldownRequestClient = miniRpc.RegisterAction<int>(Target.Client, (NetworkUser user, int i) =>
            {
                var slot = (SkillSlot)i;

                switch (slot)
                {
                    case SkillSlot.Primary:
                        AlterCooldown(localPlayerPrimarySkill, currentRunConfig.primaryAmount);
                        break;
                    case SkillSlot.Secondary:
                        AlterCooldown(localPlayerSecondarySkill, currentRunConfig.secondaryAmount);
                        break;
                    case SkillSlot.Utility:
                        AlterCooldown(localPlayerUtilitySkill, currentRunConfig.utilityAmount);
                        break;
                    case SkillSlot.Special:
                        AlterCooldown(localPlayerSpecialSkill, currentRunConfig.specialAmount);
                        break;
                    case SkillSlot.None:
                        if (DebugActive) Debug.LogError("Sent Skillslot None");
                        break;
                }
            });

        }

        public void Update()
        {
            if (!DebugActive) return;
            if (Input.GetKeyDown(KeyCode.F2))
            {
                GetLocalPlayerInfo();

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
                if (DebugActive) Chat.AddMessage(stringerino);


                if (DebugActive) Chat.AddMessage("Survivor: " + localPlayerSurvivorIndex.ToString() + " supported? " + localPlayercharacterSupported.ToString());
            }
        }

        public void GetLocalPlayerInfo()
        {
            if (NetworkServer.active)
            {
                if (DebugActive) Debug.Log("Getting LocalPlayerCharacterMaster on server");
                localPlayerCharacterMaster = LocalUserManager.GetFirstLocalUser().cachedMasterController.master;
                if (DebugActive) Debug.Log($"Got [{localPlayerCharacterMaster}])");
            }
            else if (NetworkClient.active)
            {
                if (DebugActive) Debug.Log("Getting LocalPlayerCharacterMaster on client");
                localPlayerCharacterMaster = LocalUserManager.GetFirstLocalUser().cachedMasterController.master;
                if (DebugActive) Debug.Log($"Got [{localPlayerCharacterMaster}])");
            }

            if (localPlayerCharacterMaster == null)
            {
                if (DebugActive) Debug.LogWarning("CharacterMaster not set!");
            }

            localPlayerSkillLoc = GetSkillLoc(localPlayerCharacterMaster);

            Chat_GetLocalPlayerInfo();

            GetLocalPlayerSkills();

        }

        private void GetLocalPlayerSkills()
        {
            localPlayerPrimarySkill = localPlayerSkillLoc.primary;
            localPlayerSecondarySkill = localPlayerSkillLoc.secondary;
            localPlayerUtilitySkill = localPlayerSkillLoc.utility;
            localPlayerSpecialSkill = localPlayerSkillLoc.special;
        }


        public void Chat_GetLocalPlayerInfo()
        {
            if (DebugActive) Chat.AddMessage("You are playing as: " + localPlayerSurvivorIndex);
        }



        private void ReduceCooldown(GenericSkill skill, float amount)
        {
            AlterSkillCooldown(skill, amount);
        }

        public static SkillLocator GetSkillLoc(CharacterMaster master)
        {
            if (DebugActive) Debug.Log("[GetSkillLoc]: Master: " + master.ToString());
            CharacterBody body = master.GetBody();
            if (DebugActive) Debug.Log("[GetSkillLoc]: Character Body: " + body.ToString());
            SkillLocator locator = body.GetComponent<SkillLocator>();
            if (DebugActive) Debug.Log("[GetSkillLoc]: Primary skill: " + locator.primary.name.ToString());
            return locator;
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
                if (DebugActive) Debug.Log("Hit a monster!");
            }

            return b;
        }

        public bool CanAffectCooldown(GenericSkill skill)
        {
            if (
                (skill == localPlayerPrimarySkill && currentRunConfig.primaryAmount == 0) ||
                (skill == localPlayerSecondarySkill && currentRunConfig.secondaryAmount == 0) ||
                (skill == localPlayerUtilitySkill && currentRunConfig.utilityAmount == 0) ||
                (skill == localPlayerSpecialSkill && currentRunConfig.specialAmount == 0))
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

            if (!localPlayercharacterSupported)
            {
                orig(skill, dt);
                return;
            }

            if (CanRecharge(skill))
            {
                orig(skill, dt);
            }
            else if (CanAddCharge(skill))
            {
                AddCharge(skill);
            }

            if (CanAffectCooldown(skill))
            {
                AffectCooldown(skill, dt);
            }
        }

        private void AffectCooldown(GenericSkill skill, float dt)
        {
            if (skill.stock >= skill.maxStock) return;

            // Not entirely sure about this next line..
            var dt2 = Time.fixedDeltaTime;
            if (dt == dt2) return;

            ReduceCooldown(skill, dt);
        }


        // TODO: This has to be redone - Create my own 'StopWatch'.
        public void AlterSkillCooldown(GenericSkill skill, float amount)
        {
            var skillType = typeof(GenericSkill);

            var rechargeStopwatchField = skillType.GetField("rechargeStopwatch", BindingFlags.NonPublic | BindingFlags.Instance);
            float rechargeStopwatchValue = (float)rechargeStopwatchField.GetValue(skill);

            if (newRechargeStopwatch == float.NaN) newRechargeStopwatch = GetPrivateFloatFromGenericSkillReflection(skill, "rechargeStopwatch");
            if (newFinalRechargeInterval == float.NaN) newFinalRechargeInterval = GetPrivateFloatFromGenericSkillReflection(skill, "finalRechargeInterval");

            if (!skill.beginSkillCooldownOnSkillEnd || (skill.stateMachine.state.GetType() != skill.activationState.stateType))
            {
                var finalValue = rechargeStopwatchValue + amount;
                rechargeStopwatchField.SetValue(skill, finalValue);
            }
            if (CanAddCharge(skill))
            {
                AddCharge(skill);
            }
        }

        private void AddCharge(GenericSkill skill)
        {
            var skillType = typeof(GenericSkill);
            var restockSteplikeMethod = skillType.GetMethod("RestockSteplike", BindingFlags.NonPublic | BindingFlags.Instance);
            restockSteplikeMethod.Invoke(skill, null);
        }

        private static bool CanAddCharge(GenericSkill skill)
        {
            var skillType = typeof(GenericSkill);

            var rechargeStopwatchField = skillType.GetField("rechargeStopwatch", BindingFlags.NonPublic | BindingFlags.Instance);
            float rechargeStopwatchValue = (float)rechargeStopwatchField.GetValue(skill);
            var finalRechargeIntervalField = skillType.GetField("finalRechargeInterval", BindingFlags.NonPublic | BindingFlags.Instance);
            float finalRechargeIntervalValue = (float)finalRechargeIntervalField.GetValue(skill);

            return (rechargeStopwatchValue >= finalRechargeIntervalValue);
        }
        private void CheckLocalCharacterSupported()
        {
            CharacterSupportedRequestHost.Invoke((int)localPlayerSurvivorIndex, result =>
            {
                if (DebugActive) Debug.Log(localPlayerSurvivorIndex.ToString());
                localPlayercharacterSupported = result.supported;

                localPlayerSurvivorIndex = (SurvivorIndex)result.index;

                if (DebugActive) Debug.Log($"[Client]: Server sent me (Index: {result.index}, result: {result.supported} on my request on CharacterSupport.");
            });
        }

        public bool CheckCharacterSupport(SurvivorIndex index)
        {
            var find = Array.IndexOf<SurvivorIndex>(workingSurvivors, index);

            if (find == -1)
            {
                return false;
            }
            else
            {
                return true;
            }

        }

        public SurvivorIndex GetSurvivorIndex(CharacterMaster master)
        {
            var bodyPrefab = master.bodyPrefab;
            if (DebugActive) Debug.Log("Body prefab:" + bodyPrefab);
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
            if (DebugActive) Chat.AddMessage(line0 + "\n" + line1 + "\n" + line2 + "\n" + line3);
        }

        private void SendAlterCooldownRequestToClient(SkillSlot slot, NetworkUser user)
        {
            CooldownRequestClient.Invoke((int)slot, (user));
        }

        private void ArrowOrb_OnArrival(On.RoR2.Orbs.ArrowOrb.orig_OnArrival orig, RoR2.Orbs.ArrowOrb self)
        {
            orig(self);

            if (currentRunConfig.secondaryAmount == 0) return;

            SkillLocator skillLocator = self.attacker.GetComponent<SkillLocator>();
            GenericSkill skill = skillLocator.secondary;

            CreateAlterCooldownRequest(skillLocator, skill);

        }

        private void CreateAlterCooldownRequest(SkillLocator skillLocator, GenericSkill skill)
        {

            SkillSlot slot = skillLocator.FindSkillSlot(skill);
            NetworkUser user = GetUser(skillLocator);
            SendAlterCooldownRequestToClient(slot, user);
        }

        private static NetworkUser GetUser(SkillLocator skillLocator)
        {
            var playerBody = skillLocator.GetComponent<CharacterBody>();
            NetworkUser user = null;
            var instancesList = NetworkUser.readOnlyInstancesList;
            for (int i = 0; i < instancesList.Count; i++)
            {
                if (instancesList[i].GetCurrentBody() == playerBody)
                {
                    user = instancesList[i];
                }
            }
            if (user == null)
            {
                if (DebugActive) Debug.LogWarning("User is null");
            }

            return user;
        }

        private void LightningOrb_OnArrival(On.RoR2.Orbs.LightningOrb.orig_OnArrival orig, RoR2.Orbs.LightningOrb self)
        {
            orig(self);

            if (DebugActive) Debug.Log("Lightning orb has arrived");


            if (self.lightningType == RoR2.Orbs.LightningOrb.LightningType.HuntressGlaive)
            {

                if (currentRunConfig.specialAmount == 0) return;
                var skillLocator = self.attacker.GetComponent<SkillLocator>();
                var skill = skillLocator.special;

                CreateAlterCooldownRequest(skillLocator, skill);
            }
        }
        private bool CanRecharge(GenericSkill skill)
        {
            if (
                (skill == localPlayerPrimarySkill && currentRunConfig.primaryRecharging) ||
                (skill == localPlayerSecondarySkill && currentRunConfig.secondaryRecharging) ||
                (skill == localPlayerUtilitySkill && currentRunConfig.utilityRecharging) ||
                (skill == localPlayerSpecialSkill && currentRunConfig.specialRecharging))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        [ConCommand(commandName = "coh_primary", flags = ConVarFlags.None, helpText = "Primary Skill configurations.")]
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

                if (NetworkServer.active)
                {
                    LoadConfigFromFile();
                }

                Debug.Log(savedConfig.GetPrimary + "\nThis only affects the current run if you are a host. Otherwise it just changes the config file.");
            }
            else
            {
                Debug.Log("Two arguments expected. A boolean (true or False), and a positive float (any number 0 or higher)");
            }
        }

        //[ConCommand(commandName = "coh_secondary", flags = ConVarFlags.None, helpText = "Secondary Skill configurations.")]
        //private static void CCSecondary(ConCommandArgs args)
        //{
        //    if (args.Count == 0)
        //    {
        //         Debug.Log(savedConfig.GetSecondary);
        //    }
        //    else if (args.Count == 2)
        //    {
        //        if (!bool.TryParse(args[0], out var recharging))
        //        {
        //             Debug.Log("First argument was invalid. It should be a boolean (True / False)");
        //            return;
        //        }
        //        if (!float.TryParse(args[1], out var amount))
        //        {
        //             Debug.Log("Second argument was invalid. It should be a positive float (any number 0 or above)");
        //            return;
        //        }

        //        savedConfig.SetSecondary(recharging, amount);
        //        SaveConfigToFile(savedConfig);

        //        if (NetworkServer.active)
        //        {
        //            LoadConfigFromFile();
        //        }

        //         Debug.Log(savedConfig.GetSecondary + "\nThis only affects the current run if you are a host. Otherwise it just changes the config file.");
        //    }
        //    else
        //    {
        //         Debug.Log("Two arguments expected. A boolean (true or False), and a positive float (any number 0 or higher)");
        //    }
        //}

        //[ConCommand(commandName = "coh_utility", flags = ConVarFlags.None, helpText = "Utility Skill configurations.")]
        //private static void CCUtility(ConCommandArgs args)
        //{
        //    if (args.Count == 0)
        //    {
        //         Debug.Log(savedConfig.GetUtility);
        //    }
        //    else if (args.Count == 2)
        //    {
        //        if (!bool.TryParse(args[0], out var recharging))
        //        {
        //             Debug.Log("First argument was invalid. It should be a boolean (True / False)");
        //            return;
        //        }
        //        if (!float.TryParse(args[1], out var amount))
        //        {
        //             Debug.Log("Second argument was invalid. It should be a positive float (any number 0 or above)");
        //            return;
        //        }

        //        savedConfig.SetUtility(recharging, amount);
        //        SaveConfigToFile(savedConfig);

        //        if (NetworkServer.active)
        //        {
        //            LoadConfigFromFile();
        //        }

        //         Debug.Log(savedConfig.GetUtility + "\nThis only affects the current run if you are a host. Otherwise it just changes the config file.");
        //    }
        //    else
        //    {
        //         Debug.Log("Two arguments expected. A boolean (true or False), and a positive float (any number 0 or higher)");
        //    }
        //     Debug.Log("One arguments expected. A boolean (true or False)");
        //}

        //[ConCommand(commandName = "coh_special", flags = ConVarFlags.None, helpText = "Sets Special Skill configurations.")]
        //private static void CCSpecial(ConCommandArgs args)
        //{
        //    if (args.Count == 0)
        //    {
        //         Debug.Log(savedConfig.GetSpecial);
        //    }
        //    else if (args.Count == 2)
        //    {
        //        if (!bool.TryParse(args[0], out var recharging))
        //        {
        //             Debug.Log("First argument was invalid. It should be a boolean (True / False)");
        //            return;
        //        }
        //        if (!float.TryParse(args[1], out var amount))
        //        {
        //             Debug.Log("Second argument was invalid. It should be a positive float (any number 0 or above)");
        //            return;
        //        }

        //        savedConfig.SetSpecial(recharging, amount);
        //        SaveConfigToFile(savedConfig);

        //        if (NetworkServer.active)
        //        {
        //            LoadConfigFromFile();
        //        }

        //         Debug.Log(savedConfig.GetSpecial + "\nThis only affects the current run if you are a host. Otherwise it just changes the config file.");
        //    }
        //    else
        //    {
        //         Debug.Log("Two arguments expected. A boolean (true or False), and a positive float (any number 0 or higher)");
        //    }
        //     Debug.Log("One arguments expected. A boolean (true or False)");
        //}

        //[ConCommand(commandName = "coh_equipment", flags = ConVarFlags.None, helpText = "Sets Equipment Skill configurations.")]
        //private static void CCSetEquipment(ConCommandArgs args)
        //{
        //    if (args.Count == 0)
        //    {
        //         Debug.Log(savedConfig.GetEquipment);
        //    }
        //    else if (args.Count == 2)
        //    {
        //        if (!bool.TryParse(args[0], out var recharging))
        //        {
        //             Debug.Log("First argument was invalid. It should be a boolean (True / False)");
        //            return;
        //        }
        //        if (!float.TryParse(args[1], out var amount))
        //        {
        //             Debug.Log("Second argument was invalid. It should be a positive float (any number 0 or above)");
        //            return;
        //        }

        //        savedConfig.SetEquipment(recharging, amount);
        //        SaveConfigToFile(savedConfig);

        //        if (NetworkServer.active)
        //        {
        //            LoadConfigFromFile();
        //        }

        //         Debug.Log(savedConfig.GetEquipment + "\nThis only affects the current run if you are a host. Otherwise it just changes the config file.");
        //    }
        //    else
        //    {
        //         Debug.Log("Two arguments expected. A boolean (true or False), and a positive float (any number 0 or higher)");
        //    }
        //     Debug.Log("One arguments expected. A boolean (true or False)");
        //}

        //[ConCommand(commandName = "coh_getSavedConfig", flags = ConVarFlags.None, helpText = "Displays the saved configuration.")]
        //private static void CCGetSavedConfig(ConCommandArgs args)
        //{
        //     Debug.Log(args.Count == 0  ? savedConfig.ToString() : "Does not accept arguments. Did you mean something else?");
        //}

        //[ConCommand(commandName = "coh_getRunConfig", flags = ConVarFlags.None, helpText = "Display the current configuration")]
        //private static void CCGetCurrentConfig(ConCommandArgs args)
        //{
        //     Debug.Log(args.Count == 0 ? currentRunConfig.ToString() : "Does not accept arguments. Did you mean something else?");
        //}

        //[ConCommand(commandName = "coh_resetConfig", flags = ConVarFlags.None, helpText = "Sets the config to default values.")]
        //private static void CCResetConfig(ConCommandArgs args)
        //{
        //    SaveConfigToFile(savedConfig);

        //    if (NetworkServer.active)
        //    {
        //        LoadConfigFromFile();
        //    }
        //     Debug.Log(savedConfig.ToString() + "\nThis only affects the current run if you are a host. Otherwise it just changes the config file.");
        //}




        public static ConfigContainer savedConfig = new ConfigContainer();

        public static ConfigContainer currentRunConfig;

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
            PrimarySkillRechargingConfig.Value = container.primaryRecharging;
            PrimarySkillCooldownReductionOnHitAmountConfig.Value = container.primaryAmount.ToString();

            SecondarySkillRechargingConfig.Value = container.secondaryRecharging;
            SecondarySkillCooldownReductionOnHitAmountConfig.Value = container.secondaryAmount.ToString();

            UtilitySkillRechargingConfig.Value = container.utilityRecharging;
            UtilitySkillCooldownReductionOnHitAmountConfig.Value = container.utilityAmount.ToString();

            SpecialSkillRechargingConfig.Value = container.specialRecharging;
            SpecialSkillCooldownReductionOnHitAmountConfig.Value = container.specialAmount.ToString();

            EquipmentRechargingConfig.Value = container.equipmentRecharging;
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