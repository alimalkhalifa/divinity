using System;
using Server;
using Server.Items;
using Server.Spells;
using Server.Spells.Necromancy;
using Server.Spells.Ninjitsu;
using Server.Mobiles;

namespace Server.Misc
{
	public class RegenRates
	{
		[CallPriority( 10 )]
		public static void Configure()
		{
			Mobile.DefaultHitsRate = TimeSpan.FromSeconds( 11.0 );
			Mobile.DefaultStamRate = TimeSpan.FromSeconds(  7.0 );
			Mobile.DefaultManaRate = TimeSpan.FromSeconds(  7.0 );

			Mobile.ManaRegenRateHandler = new RegenRateHandler( Mobile_ManaRegenRate );

			if ( Core.AOS )
			{
				Mobile.StamRegenRateHandler = new RegenRateHandler( Mobile_StamRegenRate );
				Mobile.HitsRegenRateHandler = new RegenRateHandler( Mobile_HitsRegenRate );
			}
		}

		private static void CheckBonusSkill( Mobile m, int cur, int max, SkillName skill )
		{
			if ( !m.Alive )
				return;

			double n = (double)cur / max;
			double v = Math.Sqrt( m.Skills[skill].Value * 0.005 );

			n *= (1.0 - v);
			n += v;

			m.CheckSkill( skill, n );
		}

		private static bool CheckTransform( Mobile m, Type type )
		{
			return TransformationSpellHelper.UnderTransformation( m, type );
		}

		private static bool CheckAnimal( Mobile m, Type type )
		{
			return AnimalForm.UnderTransformation( m, type );
		}

		private static TimeSpan Mobile_HitsRegenRate( Mobile from )
		{
			int points = AosAttributes.GetValue( from, AosAttribute.RegenHits );

			if ( from is BaseCreature && !((BaseCreature)from).IsAnimatedDead )
				points += 4;

			if ( (from is BaseCreature && ((BaseCreature)from).IsParagon) || from is Leviathan )
				points += 40;

			if ( CheckTransform( from, typeof( HorrificBeastSpell ) ) )
				points += 20;

			if ( CheckAnimal( from, typeof( Dog ) ) || CheckAnimal( from, typeof( Cat ) ) )
				points += from.Skills[SkillName.Ninjitsu].Fixed / 300;
			//TODO: What's the new increased rate?

			if( Core.ML && from.Race == Race.Human )	//Is this affected by the cap?
				points += 2;

			if ( points < 0 )
				points = 0;

			if( Core.ML && from is PlayerMobile )	//does racial bonus go before/after?
				points = Math.Min ( points, 18 );

			return TimeSpan.FromSeconds( 1.0 / (0.1 * (1 + points)) );
		}

		private static TimeSpan Mobile_StamRegenRate( Mobile from )
		{
			if ( from.Skills == null )
				return Mobile.DefaultStamRate;

			CheckBonusSkill( from, from.Stam, from.StamMax, SkillName.Focus );

			int points =(int)(from.Skills[SkillName.Focus].Value * 0.1);

			if( (from is BaseCreature && ((BaseCreature)from).IsParagon) || from is Leviathan )
				points += 40;

			int cappedPoints = AosAttributes.GetValue( from, AosAttribute.RegenStam );

			if ( CheckTransform( from, typeof( VampiricEmbraceSpell ) ) )
				cappedPoints += 15;

			if ( CheckAnimal( from, typeof( Kirin ) ) )
				cappedPoints += 20;

			if( Core.ML && from is PlayerMobile )
				cappedPoints = Math.Min( cappedPoints, 24 );

			points += cappedPoints;

			if ( points < -1 )
				points = -1;

			return TimeSpan.FromSeconds( 1.0 / (0.1 * (2 + points)) );
		}

        private static TimeSpan Mobile_ManaRegenRate(Mobile from)
        {
            if (from.Skills == null)
                return Mobile.DefaultManaRate;

            double rate;
            double armorPenalty = GetArmorOffset(from);
            double medPoints = (from.Int + from.Skills[SkillName.Meditation].Value) * 0.5;

            if (medPoints <= 0)
                rate = 7.0;
            else 
                rate = 7.0 - (239 * medPoints / 2400) + (19 * medPoints * medPoints / 48000);

            rate += armorPenalty;

            if (from.Meditating)
                rate *= 0.5;

            if (rate < 0.5)
                rate = 0.5;
            else if (rate > 7.0)
                rate = 7.0;

			return TimeSpan.FromSeconds( rate );
		}

		public static double GetArmorOffset( Mobile from )
		{
			double rating = 0.0;

			rating += GetArmorMeditationValue( from.NeckArmor as BaseArmor );
			rating += GetArmorMeditationValue( from.HandArmor as BaseArmor );
			rating += GetArmorMeditationValue( from.HeadArmor as BaseArmor );
			rating += GetArmorMeditationValue( from.ArmsArmor as BaseArmor );
			rating += GetArmorMeditationValue( from.LegsArmor as BaseArmor );
			rating += GetArmorMeditationValue( from.ChestArmor as BaseArmor );

			return rating / 4;
		}

		private static double GetArmorMeditationValue( BaseArmor ar )
		{
			if ( ar == null || ar.ArmorAttributes.MageArmor != 0 || ar.Attributes.SpellChanneling != 0 )
				return 0.0;

			double d = 0.0;
			
			switch ( ar.MeditationAllowance )
			{
				default:
				case ArmorMeditationAllowance.None: d = ar.BaseArmorRatingScaled; break;
				case ArmorMeditationAllowance.Half: d =  ar.BaseArmorRatingScaled / 2.0; break;
				case ArmorMeditationAllowance.All:  d = 0.0; break;
			}
			
            if ( ar is RangerArms || ar is RangerChest || ar is RangerGloves || ar is RangerGorget )
                return d * 0.5;

            return d;
        }
	}
}