using Modding;

namespace Exaltation
{
	public class SaveSettings : IModSettings
	{
		public bool PantheonOneGlory { get => GetBool(); set => SetBool(value); }
		public bool PantheonTwoGlory { get => GetBool(); set => SetBool(value); }
		public bool PantheonThreeGlory { get => GetBool(); set => SetBool(value); }
		public bool PantheonFourGlory { get => GetBool(); set => SetBool(value); }

		public bool GatheringSwarmGlorified { get => GetBool(); set => SetBool(value); }
		public bool WaywardCompassGlorified { get => GetBool(); set => SetBool(value); }
		public bool GrubsongGlorified { get => GetBool(); set => SetBool(value); }
		public bool StalwartShellGlorified { get => GetBool(); set => SetBool(value); }
		public bool BaldurShellGlorified { get => GetBool(); set => SetBool(value); }
		public bool FuryOfTheFallenGlorified { get => GetBool(); set => SetBool(value); }
		public bool QuickFocusGlorified { get => GetBool(); set => SetBool(value); }
		public bool LifebloodHeartGlorified { get => GetBool(); set => SetBool(value); }
		public bool LifebloodCoreGlorified { get => GetBool(); set => SetBool(value); }
		public bool SharpShadowGlorified { get => GetBool(); set => SetBool(value); }
		public bool ShamanStoneGlorified { get => GetBool(); set => SetBool(value); }
		public bool SoulCatcherGlorified { get => GetBool(); set => SetBool(value); }
		public bool SoulEaterGlorified { get => GetBool(); set => SetBool(value); }
		public bool JonisBlessingGlorified { get => GetBool(); set => SetBool(value); }
		public bool HivebloodGlorified { get => GetBool(); set => SetBool(value); }
		public bool DashmasterGlorified { get => GetBool(); set => SetBool(value); }
		public bool QuickSlashGlorified { get => GetBool(); set => SetBool(value); }
		public bool SpellTwisterGlorified { get => GetBool(); set => SetBool(value); }
		public bool SprintmasterGlorified { get => GetBool(); set => SetBool(value); }

		public bool FotFShade { get => GetBool(); set => SetBool(value); }
	}
}
