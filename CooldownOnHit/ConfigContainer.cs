using System;
using UnityEngine.Networking;


namespace CooldownOnHit
{
    public class ConfigContainer : MessageBase
    {
        public bool primaryRecharging;
        public bool secondaryRecharging;
        public bool utilityRecharging;
        public bool specialRecharging;
        public bool equipmentRecharging;


        public float primaryAmount;
        public float secondaryAmount;
        public float utilityAmount;
        public float specialAmount;
        public float equipmentAmount;


        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(primaryRecharging);
            writer.Write(secondaryRecharging);
            writer.Write(utilityRecharging);
            writer.Write(specialRecharging);
            writer.Write(equipmentRecharging);

            writer.Write(primaryAmount);
            writer.Write(secondaryAmount);
            writer.Write(utilityAmount);
            writer.Write(specialAmount);
            writer.Write(equipmentAmount);


        }

        public override void Deserialize(NetworkReader reader)
        {
            primaryRecharging = reader.ReadBoolean();
            secondaryRecharging = reader.ReadBoolean();
            utilityRecharging = reader.ReadBoolean();
            specialRecharging = reader.ReadBoolean();
            equipmentRecharging = reader.ReadBoolean();

            primaryAmount = reader.ReadSingle();
            secondaryAmount = reader.ReadSingle();
            utilityAmount = reader.ReadSingle();
            specialAmount = reader.ReadSingle();
            equipmentAmount = reader.ReadSingle();
        }


        public String GetPrimary {
            get => $"Primary Skills recharging: {   (primaryRecharging ? "Recharging" : "Not recharging")}: {primaryAmount} seconds";
        }
        public String GetSecondary {
            get => $"Secondary Skills recharging: { (secondaryRecharging ? "Recharging" : "Not recharging")}: {secondaryAmount} seconds";
        }
        public String GetUtility {
            get => $"Utility Skills recharging: {   (utilityRecharging ? "Recharging" : "Not recharging")}: {utilityAmount} seconds";
        }
        public String GetSpecial {
            get => $"Special Skills recharging: {   (specialRecharging ? "Recharging" : "Not recharging")}: {specialAmount} seconds";
        }
        public String GetEquipment {
            get => $"Equipment Skills recharging: { (equipmentRecharging ? "Recharging" : "Not recharging")}: {equipmentAmount} seconds";
        }

        public void SetPrimary(bool b, float f)
        {
            SetPrimaryRecharging(b);
            SetPrimaryAmount(f);
        }

        public void SetPrimaryRecharging(bool b)
        {
            this.primaryRecharging = b;
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
            this.secondaryRecharging = b;
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
            this.utilityRecharging = b;
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
            this.specialRecharging = b;
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
            this.equipmentRecharging = b;
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
                $"Primary Skills recharging: {   (primaryRecharging ? t : f)}: {primaryAmount} seconds\n" +
                $"Secondary Skills recharging: { (secondaryRecharging ? t : f)}: {secondaryAmount} seconds\n" +
                $"Utility Skills recharging: {   (utilityRecharging ? t : f)}: {utilityAmount} seconds\n" +
                $"Special Skills recharging: {   (specialRecharging ? t : f)}: {specialAmount} seconds\n" +
                $"Equipment Skills recharging: { (equipmentRecharging ? t : f)}: {equipmentAmount} seconds";
        }

        public void SetDefault()
        {
            primaryRecharging = true;
            primaryAmount = 0;

            secondaryRecharging = false;
            secondaryAmount = 1;

            utilityRecharging = true;
            utilityAmount = 0;

            specialRecharging = false;
            specialAmount = 3;

            equipmentRecharging = true;
            equipmentAmount = 0;

        }
    }

}