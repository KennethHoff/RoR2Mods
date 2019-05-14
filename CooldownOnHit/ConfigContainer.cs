using System;
using UnityEngine.Networking;


namespace CooldownOnHit
{
    public class ConfigContainer : MessageBase
    {

        public int primaryRecharging;
        public int secondaryRecharging;
        public int utilityRecharging;
        public int specialRecharging;
        public int equipmentRecharging;




        public float primaryAmount;
        public float secondaryAmount;
        public float utilityAmount;
        public float specialAmount;
        public float equipmentAmount;









        public String GetPrimary {
            get => $"Primary Skills recharging: {   (primaryRecharging == 1 ? "Recharging" : "Not recharging")}: {primaryAmount} seconds";
        }
        public String GetSecondary {
            get => $"Secondary Skills recharging: { (secondaryRecharging == 1 ? "Recharging" : "Not recharging")}: {secondaryAmount} seconds";
        }
        public String GetUtility {
            get => $"Utility Skills recharging: {   (utilityRecharging == 1? "Recharging" : "Not recharging")}: {utilityAmount} seconds";
        }
        public String GetSpecial {
            get => $"Special Skills recharging: {   (specialRecharging == 1 ? "Recharging" : "Not recharging")}: {specialAmount} seconds";
        }
        public String GetEquipment {
            get => $"Equipment Skills recharging: { (equipmentRecharging == 1 ? "Recharging" : "Not recharging")}: {equipmentAmount} seconds";
        }

        public void SetPrimary(bool b, float f)
        {
            SetPrimaryRecharging(b);
            SetPrimaryAmount(f);
        }

        public void SetPrimaryRecharging(bool b)
        {
            this.primaryRecharging = b ? 1 : 0;
        }

        public void SetPrimaryAmount(float f)
        {
            this.primaryAmount = f;
        }

        public void SetSecondary(bool b, float f)
        {
            SetSecondaryRecharging(b);
            SetSecondaryAmount(f);
        }

        public void SetSecondaryRecharging(bool b)
        {
            this.secondaryRecharging = b ? 1 : 0;
        }

        public void SetSecondaryAmount(float f)
        {
            this.secondaryAmount = f;
        }

        public void SetUtility(bool b, float f)
        {
            SetUtilityRecharging(b);
            SetUtilityAmount(f);
        }

        public void SetUtilityRecharging(bool b)
        {
            this.utilityRecharging = b ? 1 : 0;
        }

        public void SetUtilityAmount(float f)
        {
            this.utilityAmount = f;
        }

        public void SetSpecial(bool b, float f)
        {
            SetSpecialRecharging(b);
            SetSpecialAmount(f);
        }

        public void SetSpecialRecharging(bool b)
        {
            this.specialRecharging = b ? 1 : 0;
        }

        public void SetSpecialAmount(float f)
        {
            this.specialAmount = f;
        }

        public void SetEquipment(bool b, float f)
        {
            SetEquipmentRecharging(b);
            SetEquipmentAmount(f);
        }

        public void SetEquipmentRecharging(bool b)
        {
            this.equipmentRecharging = b ? 1 : 0;
        }

        public void SetEquipmentAmount(float f)
        {
            this.equipmentAmount = f;
        }

        public void SetConfig(bool b1, float f1, bool b2, float f2, bool b3, float f3, bool b4, float f4, bool b5, float f5)
        {
            SetPrimary(b1, f1);
            SetSecondary(b2, f2);
            SetUtility(b3, f3);
            SetSpecial(b4, f4);
            SetEquipment(b5, f5);
        }

        public override string ToString()
        {
            var t = "Recharging";
            var f = "Not recharging";
            return
                $"Current config is:\n" +
                $"Primary Skills recharging: {   (primaryRecharging == 1 ? t : f)}: {primaryAmount} seconds\n" +
                $"Secondary Skills recharging: { (secondaryRecharging == 1 ? t : f)}: {secondaryAmount} seconds\n" +
                $"Utility Skills recharging: {   (utilityRecharging == 1 ? t : f)}: {utilityAmount} seconds\n" +
                $"Special Skills recharging: {   (specialRecharging == 1 ? t : f)}: {specialAmount} seconds\n" +
                $"Equipment Skills recharging: { (equipmentRecharging == 1 ? t : f)}: {equipmentAmount} seconds";
        }

        public void SetDefault()
        {
            primaryRecharging = 1;
            primaryAmount = 0;

            secondaryRecharging = 0;
            secondaryAmount = 1;

            utilityRecharging = 1;
            utilityAmount = 0;

            specialRecharging = 0;
            specialAmount = 3;

            equipmentRecharging = 1;
            equipmentAmount = 0;

        }
    }

}