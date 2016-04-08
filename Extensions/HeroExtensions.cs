﻿namespace Ensage.Common.Extensions
{
    using System.Collections.Generic;
    using System.Linq;

    using Ensage.Common.Objects;

    /// <summary>
    ///     The hero extensions.
    /// </summary>
    public static class HeroExtensions
    {
        #region Static Fields

        /// <summary>
        ///     The boolean dictionary.
        /// </summary>
        private static readonly Dictionary<string, bool> BoolDictionary = new Dictionary<string, bool>();

        /// <summary>
        ///     The range dictionary.
        /// </summary>
        private static readonly Dictionary<float, float> RangeDictionary = new Dictionary<float, float>();

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Checks if given hero has AghanimScepter
        /// </summary>
        /// <param name="hero">
        ///     The hero.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        public static bool AghanimState(this Hero hero)
        {
            return
                hero.HasModifiers(
                    new[] { "modifier_item_ultimate_scepter_consumed", "modifier_item_ultimate_scepter" }, 
                    false);
        }

        /// <summary>
        /// The player.
        /// </summary>
        /// <param name="hero">
        /// The hero.
        /// </param>
        /// <returns>
        /// The <see cref="Player"/>.
        /// </returns>
        public static Player Player(this Hero hero)
        {
            return Players.All.FirstOrDefault(x => x.Hero != null && x.Hero.IsValid && x.Hero.Equals(hero));
        }

        /// <summary>
        ///     The attack backswing.
        /// </summary>
        /// <param name="hero">
        ///     The hero.
        /// </param>
        /// <returns>
        ///     The <see cref="double" />.
        /// </returns>
        public static double AttackBackswing(this Hero hero)
        {
            return UnitDatabase.GetAttackBackswing(hero);
        }

        /// <summary>
        ///     The attack point.
        /// </summary>
        /// <param name="hero">
        ///     The hero.
        /// </param>
        /// <returns>
        ///     The <see cref="double" />.
        /// </returns>
        public static double AttackPoint(this Hero hero)
        {
            return UnitDatabase.GetAttackPoint(hero);
        }

        /// <summary>
        ///     The attack rate.
        /// </summary>
        /// <param name="hero">
        ///     The hero.
        /// </param>
        /// <returns>
        ///     The <see cref="double" />.
        /// </returns>
        public static double AttackRate(this Hero hero)
        {
            return UnitDatabase.GetAttackRate(hero);
        }

        /// <summary>
        ///     The can die.
        /// </summary>
        /// <param name="hero">
        ///     The hero.
        /// </param>
        /// <param name="sourceAbilityName">
        ///     The source ability name.
        /// </param>
        /// <param name="ignoreReincarnation">
        ///     The ignore reincarnation.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        public static bool CanDie(this Hero hero, string sourceAbilityName = null, bool ignoreReincarnation = false)
        {
            var cullingBlade = sourceAbilityName != null && sourceAbilityName == "axe_culling_blade";
            return !ignoreReincarnation && !hero.CanReincarnate()
                   && (cullingBlade
                           ? !hero.HasModifier("modifier_skeleton_king_reincarnation_scepter_active")
                           : !hero.HasModifiers(
                               new[]
                                   {
                                       "modifier_dazzle_shallow_grave", "modifier_oracle_false_promise", 
                                       "modifier_skeleton_king_reincarnation_scepter_active"
                                   }, 
                               false));
        }

        /// <summary>
        ///     Checks if given unit can become invisible
        /// </summary>
        /// <param name="hero">
        ///     The hero.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        public static bool CanGoInvis(this Hero hero)
        {
            var n = hero.Handle + "CanGoInvis";
            if (!Utils.SleepCheck(n))
            {
                return BoolDictionary[n];
            }

            Ability invis = null;
            Ability riki = null;
            foreach (var x in hero.Spellbook.Spells)
            {
                var name = x.StoredName();
                if (name == "bounty_hunter_wind_walk" || name == "clinkz_skeleton_walk"
                    || name == "templar_assassin_meld")
                {
                    invis = x;
                    break;
                }

                if (name == "riki_permanent_invisibility")
                {
                    riki = x;
                }
            }

            if (invis == null)
            {
                invis =
                    hero.Inventory.Items.FirstOrDefault(
                        x =>
                        x.StoredName() == "item_invis_sword" || x.StoredName() == "item_silver_edge"
                        || x.StoredName() == "item_glimmer_cape");
            }

            var canGoInvis = (invis != null && hero.CanCast() && invis.CanBeCasted())
                             || (riki != null && riki.Level > 0 && !hero.IsSilenced());
            if (!BoolDictionary.ContainsKey(n))
            {
                BoolDictionary.Add(n, canGoInvis);
            }
            else
            {
                BoolDictionary[n] = canGoInvis;
            }

            Utils.Sleep(150, n);
            return canGoInvis;
        }

        /// <summary>
        ///     The can reincarnate.
        /// </summary>
        /// <param name="hero">
        ///     The hero.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        public static bool CanReincarnate(this Hero hero)
        {
            return hero.FindItem("item_aegis") != null || hero.FindSpell("skeleton_king_reincarnation").CanBeCasted();
        }

        /// <summary>
        ///     Returns actual attack range of a hero
        /// </summary>
        /// <param name="hero">
        ///     The hero.
        /// </param>
        /// <returns>
        ///     The <see cref="float" />.
        /// </returns>
        public static float GetAttackRange(this Hero hero)
        {
            var bonus = 0.0;
            float range;
            if (RangeDictionary.TryGetValue(hero.Handle, out range)
                && !Utils.SleepCheck("Common.GetAttackRange." + hero.Handle))
            {
                return range;
            }

            var classId = hero.ClassID;
            switch (classId)
            {
                case ClassID.CDOTA_Unit_Hero_TemplarAssassin:
                    var psi = hero.Spellbook.SpellE;
                    if (psi != null && psi.Level > 0)
                    {
                        bonus = psi.GetAbilityData("bonus_attack_range");
                    }

                    break;
                case ClassID.CDOTA_Unit_Hero_Sniper:
                    var aim = hero.Spellbook.SpellE;
                    if (aim != null && aim.Level > 0)
                    {
                        bonus = aim.GetAbilityData("bonus_attack_range");
                    }

                    break;
                case ClassID.CDOTA_Unit_Hero_Enchantress:
                    var impetus = hero.Spellbook.SpellR;
                    if (impetus.Level > 0 && hero.AghanimState())
                    {
                        bonus = 190;
                    }

                    break;
                default:
                    if (hero.HasModifier("modifier_lone_druid_true_form"))
                    {
                        bonus = -423;
                    }
                    else if (hero.HasModifier("modifier_dragon_knight_dragon_form"))
                    {
                        bonus = 372;
                    }
                    else if (hero.HasModifier("modifier_terrorblade_metamorphosis"))
                    {
                        bonus = 422;
                    }

                    break;
            }

            if (hero.IsRanged)
            {
                var dragonLance = hero.FindItem("item_dragon_lance");
                if (dragonLance != null)
                {
                    bonus += dragonLance.GetAbilityData("base_attack_range");
                }
            }

            range = (float)(hero.AttackRange + bonus + (hero.HullRadius / 2));
            if (!RangeDictionary.ContainsKey(hero.Handle))
            {
                RangeDictionary.Add(hero.Handle, range);
            }
            else
            {
                RangeDictionary[hero.Handle] = range;
            }

            Utils.Sleep(500, "Common.GetAttackRange." + hero.Handle);

            return range;
        }

        /// <summary>
        ///     The is illusion.
        /// </summary>
        /// <param name="hero">
        ///     The hero.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        public static bool IsIllusion(this Hero hero)
        {
            return hero.IsIllusion;
        }

        /// <summary>
        ///     The projectile speed.
        /// </summary>
        /// <param name="hero">
        ///     The hero.
        /// </param>
        /// <returns>
        ///     The <see cref="double" />.
        /// </returns>
        public static double ProjectileSpeed(this Hero hero)
        {
            return UnitDatabase.GetProjectileSpeed(hero);
        }

        #endregion
    }
}