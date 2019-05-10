using BepInEx;
using BepInEx.Configuration;
using RoR2;
using UnityEngine;
using R2API;
using System.Reflection;

namespace Modernkennnern
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.Modernkennnern.CooldownOnHit", "CooldownOnHit", "0.1")]
    public class CooldownOnHit : BaseUnityPlugin
    {
        // Change cooldown of your secondary skill by a flat amount whenever your main skill hits a target. [NO LONGER IN USE]
        public float flatCooldownReductionOnHit;

        // Reduce cooldown of your secondary skill by a percentage of total cooldown attack whenever your main skill hits a target. [NO LONGER IN USE]
        public float percentageTotalCooldownReductionOnHit;

        public bool disablePassiveSkillCooldowns;

        public bool enableSecondaryAbilityCooldownReduction;
        public float secondaryAbilityCooldownReductionOnHit;

        public bool enableSpecialAbilityCooldownReduction;
        public float specialAbilityCooldownReductionOnHit;

        public bool enableEquipmentCooldownReduction;
        public float equipmentCooldownReductionOnHit;

        public float newRechargeStopwatch;
        public float newFinalRechargeInterval;

        public CharacterBody character;
        public float lol;

        public void Awake()
        {
            SetDefaultSettings();
            On.RoR2.Orbs.LightningOrb.OnArrival += LightningOrb_OnArrival;
            On.RoR2.Orbs.ArrowOrb.OnArrival += ArrowOrb_OnArrival;
            On.RoR2.GenericSkill.RunRecharge += GenericSkill_RunRecharge;
        }

        private void GenericSkill_RunRecharge(On.RoR2.GenericSkill.orig_RunRecharge orig, GenericSkill self, float dt)
        {
            if (character == null)
            {
                character = PlayerCharacterMasterController.instances[0].master.GetBody();
            }
            var skillLocator = character.GetComponent<SkillLocator>();
            if (disablePassiveSkillCooldowns)
            {
                if (skillLocator.FindSkillSlot(self) == SkillSlot.Secondary || skillLocator.FindSkillSlot(self) == SkillSlot.Special)
                {
                    if (self.stock < self.maxStock)
                    {
                        //Chat.AddMessage("Secondary or Special currently on cooldown");
                        var skillType = typeof(RoR2.GenericSkill);

                        var rechargeStopwatchField = skillType.GetField("rechargeStopwatch", BindingFlags.NonPublic | BindingFlags.Instance);
                        var finalRechargeIntervalField = skillType.GetField("finalRechargeInterval", BindingFlags.NonPublic | BindingFlags.Instance);
                        var restockSteplikeMethod = skillType.GetMethod("RestockSteplike", BindingFlags.NonPublic | BindingFlags.Instance);

                        if (newRechargeStopwatch == float.NaN) newRechargeStopwatch = GetPrivateFloatFromGenericSkills(self, "rechargeStopwatch");
                        if (newFinalRechargeInterval == float.NaN) newFinalRechargeInterval = GetPrivateFloatFromGenericSkills(self, "finalRechargeInterval");


                        if (!(self.beginSkillCooldownOnSkillEnd) || (self.stateMachine.state.GetType()) != (self.activationState.stateType))
                        {
                            // THE FOLLOWING LINE IS BAD - FIX THIS IN THE FUTURE
                            if (dt >= 0.25f)
                            {
                                rechargeStopwatchField.SetValue(self, (float)rechargeStopwatchField.GetValue(self) + dt);
                            }
                        }
                        if ((float)rechargeStopwatchField.GetValue(self) >= (float)finalRechargeIntervalField.GetValue(self))
                        {
                            restockSteplikeMethod.Invoke(self, null);
                        }
                        return;
                    }
                }
                else
                {
                    orig(self, dt);
                }
                return;
            }
            else
            {
                orig(self, dt);
            }
            return;
        }

        private void SetDefaultSettings()
        {
            disablePassiveSkillCooldowns = true;
            enableSecondaryAbilityCooldownReduction = true;
            enableSpecialAbilityCooldownReduction = true;
            enableEquipmentCooldownReduction = true;

            secondaryAbilityCooldownReductionOnHit = -0.3f;
            specialAbilityCooldownReductionOnHit = -3f;
            equipmentCooldownReductionOnHit = -5f;

            Chat.AddMessage("Secondary (" + enableSecondaryAbilityCooldownReduction + "): " + secondaryAbilityCooldownReductionOnHit);
            Chat.AddMessage("Special (" + enableSpecialAbilityCooldownReduction + "): " + specialAbilityCooldownReductionOnHit);
            Chat.AddMessage("Equipment (" + enableEquipmentCooldownReduction + "): " + equipmentCooldownReductionOnHit);
            Chat.AddMessage("Cooldown Disabled: " + disablePassiveSkillCooldowns.ToString());

            newFinalRechargeInterval = float.NaN;
            newRechargeStopwatch = float.NaN;
        }

        public float GetSkillCooldown(GenericSkill skill)
        {
            float value = GetPrivateFloatFromGenericSkills(skill, "finalRechargeInterval");
            return value;
        }
        public float GetRechargeTimer(GenericSkill skill)
        {
            float value = GetPrivateFloatFromGenericSkills(skill, "rechargeStopwatch");
            return value;
        }

        public float GetPrivateFloatFromGenericSkills(GenericSkill skill, string field)
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

            if (!enableSecondaryAbilityCooldownReduction) return;

            var skillLocator = self.attacker.GetComponent<SkillLocator>();
            var skill = skillLocator.secondary;
            AlterCooldownByFlatAmount(skill, -secondaryAbilityCooldownReductionOnHit);
        }
        private void LightningOrb_OnArrival(On.RoR2.Orbs.LightningOrb.orig_OnArrival orig, RoR2.Orbs.LightningOrb self)
        {
            orig(self);

            if (!enableSpecialAbilityCooldownReduction) return;

            if (self.lightningType == RoR2.Orbs.LightningOrb.LightningType.HuntressGlaive)
            {
                var skillLocator = self.attacker.GetComponent<SkillLocator>();
                var skill = skillLocator.special;
                AlterCooldownByFlatAmount(skill, -specialAbilityCooldownReductionOnHit);
            }
        }
    }
}